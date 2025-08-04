﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace UnrealSharpWeaver.NativeTypes;

public abstract class NativeDataSimpleType(WeaverImporter importer, TypeReference typeRef, string marshallerName, int arrayDim, PropertyType propertyType) 
    : NativeDataType(importer, typeRef, arrayDim, propertyType)
{
    protected TypeReference? MarshallerClass;
    protected MethodReference? ToNative;
    protected MethodReference? FromNative;
    
    private bool _isReference;
    private AssemblyDefinition? _assembly;
    
    public override bool IsPlainOldData => true;

    protected virtual TypeReference[] GetTypeParams()
    {
        return [_importer.ImportType(CSharpType)];
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        _isReference = propertyMetadata.IsOutParameter;
        var isGenericMarshaller = marshallerName.Contains('`');

        TypeReference[] typeParams = GetTypeParams();

        _importer.ForEachAssembly(definition =>
        {
            TypeReference? foundMarshaller = isGenericMarshaller
                ? _importer.FindGenericType(definition, "", marshallerName, typeParams, false) 
                : GetTypeInAssembly(definition);
            
            if (foundMarshaller is not null)
            {
                MarshallerClass = foundMarshaller;
                _assembly = definition;
                return false;
            }
            
            return true;
        });
        
        if (MarshallerClass is null)
        {
            throw new Exception($"Could not find marshaller class {marshallerName} for type {CSharpType.FullName}");
        }
        
        var marshallerTypeDefinition = GetMarshallerTypeDefinition();
        ToNative = marshallerTypeDefinition.FindMethod(_importer, "ToNative")!;
        FromNative = marshallerTypeDefinition.FindMethod(_importer, "FromNative")!;
        
        if (isGenericMarshaller)
        {
            ToNative = _importer.FunctionProcessor.MakeMethodDeclaringTypeGeneric(ToNative, typeParams);
            FromNative = _importer.FunctionProcessor.MakeMethodDeclaringTypeGeneric(FromNative, typeParams);
        }
        
        ToNative = _importer.ImportMethod(ToNative);
        FromNative = _importer.ImportMethod(FromNative);
    }

    public override void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr, FieldDefinition? fieldDefinition)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        WriteMarshalFromNative(processor, type, loadBufferPtr, processor.Create(OpCodes.Ldc_I4_0));
        getter.FinalizeMethod();
    }

    public override void WriteSetter(TypeDefinition type, MethodDefinition setter, Instruction[] loadBufferPtr, FieldDefinition? fieldDefinition)
    {
        ILProcessor processor = BeginSimpleSetter(setter);
        Instruction loadValue = processor.Create(_isReference ? OpCodes.Ldarga : OpCodes.Ldarg, 1);
        WriteMarshalToNative(processor, type, loadBufferPtr, processor.Create(OpCodes.Ldc_I4_0), loadValue);
        setter.FinalizeMethod();
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, VariableDefinition localVar)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(_importer, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stloc, localVar);
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition destField)
    {
        processor.Emit(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(_importer, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, int argIndex, ParameterDefinition paramDefinition)
    {
        // Load parameter index onto the stack. First argument is always 1, because 0 is the instance.
        List<Instruction> source = [processor.Create(OpCodes.Ldarg, argIndex)];
        
        if (_isReference)
        {
            Instruction loadInstructionOutParam = paramDefinition.CreateLoadInstructionOutParam(PropertyType);
            source.Add(loadInstructionOutParam);
        }
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(_importer, loadBuffer, offsetField);
        
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), source.ToArray());
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadField =
        [
            processor.Create(OpCodes.Ldarg_0),
            processor.Create(_isReference ? OpCodes.Ldflda : OpCodes.Ldfld, srcField)
        ];
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(_importer, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadField);
    }
    
    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSource)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        
        processor.Append(loadArrayIndex);
        
        foreach (Instruction i in loadSource)
        {
            processor.Append(i);
        }
        
        processor.Emit(OpCodes.Call, ToNative);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        
        processor.Append(loadArrayIndex);
        processor.Emit(OpCodes.Call, FromNative);
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        TypeReference[] typeParams = [];
        
        if (marshallerName.EndsWith("`1"))
        {
            if (!CSharpType.IsGenericInstance)
            {
                typeParams = [CSharpType];
            }
            else
            {
                GenericInstanceType generic = (GenericInstanceType)CSharpType;

                typeParams = [_importer.UserAssembly.MainModule.ImportReference(generic.GenericArguments[0].Resolve())];
            }
        }

        EmitSimpleMarshallerDelegates(processor, marshallerName, typeParams);
    }

    private TypeReference? GetTypeInAssembly(AssemblyDefinition assemblyDefinition)
    {
        // Try to find the marshaller in the bindings again, but with the namespace of the property type.
        TypeDefinition? propType = CSharpType.Resolve();
        TypeReference? type = _importer.FindType(assemblyDefinition, marshallerName, propType.Namespace, false);
        if (type is not null)
        {
            return type;
        }
        
        // Try to find the marshaller in the bindings assembly. These are unique so we don't need to check the namespace.
        TypeReference? typeInBindingAssembly = _importer.FindType(assemblyDefinition, marshallerName, "", false);
        if (typeInBindingAssembly is not null)
        {
            return typeInBindingAssembly;
        }

        return null;
    }

    private TypeDefinition GetMarshallerTypeDefinition()
    {
        if (MarshallerClass is null)
        {
            throw new Exception($"Marshaller class is null for type {CSharpType.FullName}");
        }
        
        if (_assembly is null)
        {
            throw new Exception($"Could not find assembly for marshaller {MarshallerClass.Name}");
        }
        
        return GetMarshallerTypeDefinition(_assembly, MarshallerClass);
    }
}