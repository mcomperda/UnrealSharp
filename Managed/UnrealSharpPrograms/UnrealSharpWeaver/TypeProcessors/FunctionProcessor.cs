using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public class FunctionProcessor(WeaverImporter importer) : BaseProcessor(importer)
{
    public void PrepareFunctionForRewrite(FunctionMetaData function, TypeDefinition classDefinition)
    {
        FieldDefinition? paramsSizeField = null;

        if (function.HasParameters)
        {
            for (int i = 0; i < function.Parameters.Length; i++)
            {
                PropertyMetaData param = function.Parameters[i];
                AddOffsetField(classDefinition, param, function, i, function.RewriteInfo.FunctionParams);
                AddNativePropertyField(classDefinition, param, function, i, function.RewriteInfo.FunctionParams);
            }

            paramsSizeField = classDefinition.AddField($"{function.Name}_ParamsSize", _importer.Int32TypeRef);
            function.RewriteInfo.FunctionParamSizeField = paramsSizeField;
        }

        if (function.ReturnValue != null)
        {
            int index = function.Parameters.Length > 0 ? function.Parameters.Length : 0;
            AddOffsetField(classDefinition, function.ReturnValue, function, index, function.RewriteInfo.FunctionParams);
            AddNativePropertyField(classDefinition, function.ReturnValue, function, index, function.RewriteInfo.FunctionParams);
        }
        
        if (function.IsBlueprintEvent || function.IsRpc)
        {
            function.FunctionPointerField = classDefinition.AddField($"{function.Name}_NativeFunction", _importer.IntPtrType, FieldAttributes.Private);
            RewriteMethodAsUFunctionInvoke(classDefinition, function, paramsSizeField, function.RewriteInfo.FunctionParams);
        }
        else if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintCallable))
        {
            foreach (var virtualFunction in classDefinition.Methods)
            {
                if (virtualFunction.Name != function.Name)
                {
                    continue;
                }

                if (virtualFunction.IsVirtual && virtualFunction.GetBaseMethod() != virtualFunction)
                {
                    continue;
                }

                MakeManagedMethodInvoker(classDefinition, function, virtualFunction, function.RewriteInfo.FunctionParams);
                break;
            }
        }
        else
        {
            MakeManagedMethodInvoker(classDefinition, function, function.MethodDef, function.RewriteInfo.FunctionParams);
        }
    }
    
    public void LoadNativeFunctionField(ILProcessor processor, FunctionMetaData functionMetaData)
    {
        if (functionMetaData.FunctionPointerField == null)
        {
            throw new InvalidOperationException("Function pointer field is null.");
        }
        
        if (functionMetaData.FunctionPointerField.IsStatic)
        {
            processor.Emit(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, functionMetaData.FunctionPointerField);
        }
    }
    
    public bool HasSameSignature(MethodReference a, MethodReference b)
    {
        if (a.Parameters.Count != b.Parameters.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Parameters.Count; i++)
        {
            if (a.Parameters[i].ParameterType.FullName != b.Parameters[i].ParameterType.FullName)
            {
                return false;
            }
        }

        return true;
    }
    
    public MethodDefinition MakeImplementationMethod(FunctionMetaData func)
    {
        MethodDefinition copiedMethod = _importer.CopyMethod(func.MethodDef.Name + "_Implementation", func.MethodDef);
        if (copiedMethod.IsVirtual)
        {
            // Find the call to the original function and replace it with a call to the implementation.
            foreach (var instruction in copiedMethod.Body.Instructions)
            {
                if (instruction.OpCode != OpCodes.Call)
                {
                    continue;
                }
                
                MethodReference calledMethod = (MethodReference) instruction.Operand;
                
                if (func.SourceName != calledMethod.Name)
                {
                    continue;
                }

                if (!copiedMethod.DeclaringType.Resolve().IsChildOf(calledMethod.DeclaringType.Resolve()) 
                    || !HasSameSignature(copiedMethod, calledMethod))
                {
                    continue;
                }

                MethodReference implementationMethod = copiedMethod.DeclaringType.BaseType.Resolve().FindMethod(_importer, copiedMethod.Name)!;
                instruction.Operand = _importer.ImportMethod(implementationMethod);
            }
        }
        return copiedMethod;
    }

    public MethodReference MakeMethodDeclaringTypeGeneric(MethodReference method, params TypeReference[] args)
    {
        if (args.Length == 0)
        {
            return method;
        }

        if (method.DeclaringType.GenericParameters.Count != args.Length)
        {
            throw new ArgumentException("Invalid number of generic type arguments supplied");
        }
        
        var genericTypeRef = method.DeclaringType.MakeGenericInstanceType(args);

        var newMethodRef = new MethodReference(method.Name, method.ReturnType, genericTypeRef)
        {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention
        };

        foreach (var parameter in method.Parameters)
        {
            newMethodRef.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        }

        foreach (var genericParam in method.GenericParameters)
        {
            newMethodRef.GenericParameters.Add(new GenericParameter(genericParam.Name, newMethodRef));
        }

        return newMethodRef;
    }

    private void MakeManagedMethodInvoker(TypeDefinition type, FunctionMetaData func, MethodDefinition methodToCall, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        MethodDefinition invokerFunction = type.AddMethod(_importer, "Invoke_" + func.Name,
            _importer.VoidTypeRef, 
            MethodAttributes.Private, 
            [_importer.IntPtrType, _importer.IntPtrType]);

        ILProcessor processor = invokerFunction.Body.GetILProcessor();
        Instruction loadBuffer = processor.Create(OpCodes.Ldarg_1);
        
        VariableDefinition[] paramVariables = new VariableDefinition[func.Parameters.Length];
        
        for (int i = 0; i < func.Parameters.Length; ++i)
        {
            PropertyMetaData param = func.Parameters[i];
            TypeReference paramType = _importer.ImportType(param.PropertyDataType.CSharpType);
            paramVariables[i] = invokerFunction.AddLocalVariable(paramType);
            
            param.PropertyDataType.PrepareForRewrite(type, param, methodToCall);

            if (param.PropertyFlags.HasFlag(PropertyFlags.OutParm))
            {
                continue;
            }

            param.PropertyDataType.WriteLoad(processor, type, loadBuffer, paramRewriteInfos[i].OffsetField!, paramVariables[i]);
        }

        OpCode callOp = OpCodes.Call;
        
        if (!methodToCall.IsStatic)
        {
            processor.Emit(OpCodes.Ldarg_0);
            if (methodToCall.IsVirtual)
            {
                callOp = OpCodes.Callvirt;
            }
        }

        for (var i = 0; i < paramVariables.Length; ++i)
        {
            VariableDefinition local = paramVariables[i];
            PropertyMetaData param = func.Parameters[i];
            OpCode loadCode = param.IsOutParameter ? OpCodes.Ldloca : OpCodes.Ldloc;
            processor.Emit(loadCode, local);
        }

        var returnIndex = 0;

        if (func.ReturnValue != null)
        {
            TypeReference returnType = _importer.ImportType(func.ReturnValue.PropertyDataType.CSharpType);
            invokerFunction.AddLocalVariable(returnType);
            returnIndex = invokerFunction.Body.Variables.Count - 1;
        }

        processor.Emit(callOp, methodToCall.ImportMethod(_importer));

        // Marshal out params back to the native parameter buffer.
        for (int i = 0; i < paramVariables.Length; ++i)
        {
            PropertyMetaData param = func.Parameters[i];
            
            if (!param.IsOutParameter)
            {
                continue;
            }
            
            VariableDefinition localVariable = paramVariables[i];
            FieldDefinition offsetField = paramRewriteInfos[i].OffsetField!;
            NativeDataType nativeDataParamType = param.PropertyDataType;

            Instruction loadLocalVariable = processor.Create(OpCodes.Ldloc, localVariable);
            nativeDataParamType.PrepareForRewrite(type, param, invokerFunction);
            
            Instruction[] loadBufferPtr = NativeDataType.GetArgumentBufferInstructions(_importer, loadBuffer, offsetField);
            
            nativeDataParamType.WriteMarshalToNative(processor, 
                type, 
                loadBufferPtr, 
                processor.Create(OpCodes.Ldc_I4_0), 
                loadLocalVariable);
        }

        if (func.ReturnValue != null)
        {
            NativeDataType nativeReturnType = func.ReturnValue.PropertyDataType;
            processor.Emit(OpCodes.Stloc, returnIndex);

            Instruction loadReturnProperty = processor.Create(OpCodes.Ldloc, returnIndex);

            nativeReturnType.PrepareForRewrite(type, func.ReturnValue, invokerFunction);
            
            nativeReturnType.WriteMarshalToNative(processor, type, [processor.Create(OpCodes.Ldarg_2)],
                processor.Create(OpCodes.Ldc_I4_0), loadReturnProperty);
        }
        
        invokerFunction.FinalizeMethod();
    }

    public void RewriteMethodAsUFunctionInvoke(TypeDefinition type, FunctionMetaData func, FieldDefinition? paramsSizeField, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        if (func.MethodDef.Body != null)
        {
            MakeManagedMethodInvoker(type, func, MakeImplementationMethod(func), paramRewriteInfos);
        }
        
        RewriteOriginalFunctionToInvokeNative(type, func, func.MethodDef, paramsSizeField, paramRewriteInfos);
    }

    public void RewriteOriginalFunctionToInvokeNative(TypeDefinition type, 
        FunctionMetaData metadata,
        MethodDefinition methodDef, 
        FieldDefinition? paramsSizeField,
        FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        // Remove the original method body. We'll replace it with a call to the native function.
        methodDef.Body = new MethodBody(methodDef);
        
        if (metadata.FunctionPointerField == null)
        {
            throw new InvalidOperationException("Function pointer field is null.");
        }

        bool staticNativeFunction = metadata.FunctionPointerField.IsStatic;
        bool hasReturnValue = !methodDef.ReturnsVoid();
        bool hasParams = methodDef.Parameters.Count > 0 || hasReturnValue;

        ILProcessor processor = methodDef.Body.GetILProcessor();
        VariableDefinition? argumentsBufferPtr = null;
        List<Instruction> allCleanupInstructions = [];
        
        Instruction loadObjectInstance = Instruction.Create(OpCodes.Ldarg_0);
        Instruction? loadArgumentBuffer = null;
        
        if (hasParams)
        {
            WriteParametersToNative(processor, methodDef, metadata, paramsSizeField, paramRewriteInfos, out argumentsBufferPtr, out loadArgumentBuffer, allCleanupInstructions);
        }
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, _importer.NativeObjectGetter);

        if (staticNativeFunction)
        {
            processor.Emit(OpCodes.Ldsfld, metadata.FunctionPointerField);
        }
        else
        {
            processor.Append(loadObjectInstance);
            processor.Emit(OpCodes.Ldfld, metadata.FunctionPointerField);
        }

        if (hasParams)
        {
            processor.Emit(OpCodes.Ldloc, argumentsBufferPtr);
        }
        else
        {
            processor.Emit(OpCodes.Ldsfld, _importer.IntPtrZero);
        }

        if (hasReturnValue)
        {
            processor.Emit(OpCodes.Ldloc, argumentsBufferPtr);
            processor.Emit(OpCodes.Ldsfld, metadata.ReturnValue!.PropertyOffsetField);
            processor.Emit(OpCodes.Call, _importer.IntPtrAdd);
        }
        else
        {
            processor.Emit(OpCodes.Ldsfld, _importer.IntPtrZero);
        }
        
        processor.Emit(OpCodes.Call, DetermineInvokeFunction(_importer, metadata));

        foreach (Instruction instruction in allCleanupInstructions)
        {
            processor.Append(instruction);
        }
        
        // Marshal out params back from the native parameter buffer.
        if (metadata.FunctionFlags.HasFlag(EFunctionFlags.HasOutParms))
        {
            for (var i = 0; i < metadata.Parameters.Length; ++i)
            {
                PropertyMetaData param = metadata.Parameters[i];

                if (!param.PropertyFlags.HasFlag(PropertyFlags.OutParm))
                {
                    continue;
                }

                processor.Emit(OpCodes.Ldarg, i + 1);

                Instruction[] load = NativeDataType.GetArgumentBufferInstructions(_importer, loadArgumentBuffer, paramRewriteInfos[i].OffsetField!);
                param.PropertyDataType.WriteMarshalFromNative(processor, type, load, processor.Create(OpCodes.Ldc_I4_0));
            
                Instruction setInstructionOutParam = methodDef.Parameters[i].CreateSetInstructionOutParam(param.PropertyDataType.PropertyType);
                processor.Append(setInstructionOutParam);
            }
        }

        // Marshal return value back from the native parameter buffer.
        if (metadata.ReturnValue != null)
        {
            // Return value is always the last parameter.
            Instruction[] load = NativeDataType.GetArgumentBufferInstructions(_importer, loadArgumentBuffer, paramRewriteInfos[^1].OffsetField!);
            metadata.ReturnValue.PropertyDataType.WriteMarshalFromNative(processor, type, load, Instruction.Create(OpCodes.Ldc_I4_0));
        }

        processor.Emit(OpCodes.Ret);
        
        if (staticNativeFunction)
        {
            return;
        }
        
        Instruction branchTarget = processor.Body.Instructions[0];
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldfld, metadata.FunctionPointerField));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldsfld, _importer.IntPtrZero));
        
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, _importer.IntPtrEqualsOperator));

        Instruction branchPosition = processor.Create(OpCodes.Ldarg_0);

        processor.InsertBefore(branchTarget, branchPosition);
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, _importer.NativeObjectGetter));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldstr, methodDef.Name));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, _importer.GetNativeFunctionFromInstanceAndNameMethod));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Stfld, metadata.FunctionPointerField));
        processor.InsertBefore(branchPosition, processor.Create(OpCodes.Brfalse, branchTarget));
        
        methodDef.OptimizeMethod();
    }

    public void RewriteMethodAsAsyncUFunctionImplementation(MethodDefinition methodDefinition)
    {
        methodDefinition.CustomAttributes.Clear();
        methodDefinition.Name = $"{methodDefinition.Name}_Implementation";
    }

    public MethodDefinition CreateMethod(TypeDefinition declaringType, string name, MethodAttributes attributes, TypeReference? returnType = null, TypeReference[]? parameters = null)
    {
        MethodDefinition def = new MethodDefinition(name, attributes, returnType ?? _importer.VoidTypeRef);

        if (parameters != null)
        {
            foreach (var type in parameters)
            {
                if (type == null)
                {
                    throw new ArgumentException("Parameter type cannot be null.", nameof(parameters));
                }
                
                def.Parameters.Add(new ParameterDefinition(type));
            }
        }

        declaringType.Methods.Add(def);
        return def;
    }
    
    public void AddOffsetField(TypeDefinition classDefinition, PropertyMetaData propertyMetaData, FunctionMetaData func, int index, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        FieldDefinition newField = classDefinition.AddField(func.Name + "_" + propertyMetaData.Name + "_Offset", _importer.Int32TypeRef);
        paramRewriteInfos[index].OffsetField = newField;
        propertyMetaData.PropertyOffsetField = newField;
    }

    public void AddNativePropertyField(TypeDefinition classDefinition, PropertyMetaData propertyMetaData, FunctionMetaData func, int index, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        if (!propertyMetaData.PropertyDataType.NeedsNativePropertyField)
        {
            return;
        }

        var newField = classDefinition.AddField(func.Name + "_" + propertyMetaData.Name + "_NativeProperty", _importer.IntPtrType,
            FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.Private);
        paramRewriteInfos[index].NativePropertyField = newField;
        propertyMetaData.NativePropertyField = newField;
    }

    public void WriteParametersToNative(ILProcessor processor, 
        MethodDefinition methodDef,
        FunctionMetaData metadata,
        FieldDefinition? paramsSizeField,
        FunctionParamRewriteInfo[] paramRewriteInfos, 
        out VariableDefinition argumentsBufferPtr, 
        out Instruction loadArgumentBuffer, 
        List<Instruction> allCleanupInstructions)
    {
        VariableDefinition argumentsBuffer = methodDef.AddLocalVariable(new PointerType(_importer.ByteTypeRef));
        methodDef.Body.Variables.Add(argumentsBuffer);
        
        processor.Emit(OpCodes.Ldsfld, paramsSizeField);
        processor.Emit(OpCodes.Conv_I4);
        processor.Emit(OpCodes.Localloc);
        processor.Emit(OpCodes.Stloc, argumentsBuffer);

        // nint num = (nint) ptr;
        //IL_0037: ldloc 0
        //IL_003b: conv.i
        //IL_003c: stloc 1
        processor.Emit(OpCodes.Ldloc, argumentsBuffer);
        processor.Emit(OpCodes.Conv_I);
        argumentsBufferPtr = methodDef.AddLocalVariable(_importer.IntPtrType);
        processor.Emit(OpCodes.Stloc, argumentsBufferPtr);
        
        // Initialize values
        LoadNativeFunctionField(processor, metadata);
        processor.Emit(OpCodes.Ldloc, argumentsBufferPtr);
        processor.Emit(OpCodes.Call, _importer.InitializeStructMethod);
        
        loadArgumentBuffer = processor.Create(OpCodes.Ldloc, argumentsBufferPtr);

        for (byte i = 0; i < paramRewriteInfos.Length; ++i)
        {
            PropertyMetaData paramType = paramRewriteInfos[i].PropertyMetaData;
            
            if (paramType.IsReturnParameter)
            {
                continue;
            }

            if (paramType is { IsOutParameter: true, IsReferenceParameter: false })
            {
                continue;
            }
        
            FieldDefinition offsetField = paramRewriteInfos[i].OffsetField!;
            NativeDataType nativeDataType = paramType.PropertyDataType;

            IList<Instruction>? cleanupInstructions = nativeDataType.WriteStore(processor, methodDef.DeclaringType, loadArgumentBuffer, offsetField, i + 1, methodDef.Parameters[i]);

            if (cleanupInstructions != null)
            {
                allCleanupInstructions.AddRange(cleanupInstructions);
            }
        }
    }

    MethodReference DetermineInvokeFunction(WeaverImporter importer, FunctionMetaData functionMetaData)
    {
        if (functionMetaData.IsRpc)
        {
            return importer.InvokeNativeNetFunction;
        }

        if (functionMetaData.HasOutParams)
        {
            return importer.InvokeNativeFunctionOutParms;
        }

        return importer.InvokeNativeFunctionMethod;
    }
    
}