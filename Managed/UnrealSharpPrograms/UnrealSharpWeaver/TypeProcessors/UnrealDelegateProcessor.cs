using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public class UnrealDelegateProcessor(WeaverImporter importer) : BaseProcessor(importer)
{
    public const string InitializeUnrealDelegate = "InitializeUnrealDelegate";
    private readonly FunctionProcessor _functionProcessor = new(importer);

    public void ProcessDelegates(List<TypeDefinition> delegates, List<TypeDefinition> multicastDelegates, AssemblyDefinition assembly, List<DelegateMetaData> delegateMetaData)
    {
        int totalDelegateCount = multicastDelegates.Count + delegates.Count;
        if (totalDelegateCount <= 0)
        {
            return;
        }
            
        delegateMetaData.Capacity = totalDelegateCount;
        
        ProcessMulticastDelegates(multicastDelegates, delegateMetaData);
        ProcessSingleDelegates(delegates, assembly, delegateMetaData);
    }
    
    private void ProcessMulticastDelegates(List<TypeDefinition> delegateExtensions, List<DelegateMetaData> delegateMetaData)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            MethodReference? invokerMethod = type.FindMethod(_importer, "Invoker", throwIfNotFound: false);
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate extension type");
            }
            
            FunctionMetaData functionMetaData = new FunctionMetaData(_importer, invokerMethod.Resolve());
            DelegateMetaData newDelegate = new DelegateMetaData(_importer, functionMetaData, 
                type, 
                "UMulticastDelegate", 
                EFunctionFlags.MulticastDelegate);
            
            delegateMetaData.Add(newDelegate);
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            WriteInvokerMethod(type, invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }
    
    private void ProcessSingleDelegates(List<TypeDefinition> delegateExtensions, AssemblyDefinition assembly, List<DelegateMetaData> delegateMetaData)
    {
        if (delegateExtensions.Count == 0)
        {
            return;
        }
        
        TypeReference delegateDataStruct = importer.FindType(_importer.UnrealSharpAssembly, "DelegateData", WeaverImporter.UnrealSharpNamespace)!;
        TypeReference blittableMarshaller = importer.FindGenericType(_importer.UnrealSharpCoreAssembly, WeaverImporter.UnrealSharpCoreMarshallers, "BlittableMarshaller`1", [delegateDataStruct])!;
        
        MethodReference blittabletoNativeMethod = blittableMarshaller.Resolve().FindMethod(_importer, "ToNative")!;
        MethodReference blittablefromNativeMethod = blittableMarshaller.Resolve().FindMethod(_importer, "FromNative")!;
        blittabletoNativeMethod = _functionProcessor.MakeMethodDeclaringTypeGeneric(blittabletoNativeMethod, [delegateDataStruct]);
        blittablefromNativeMethod = _functionProcessor.MakeMethodDeclaringTypeGeneric(blittablefromNativeMethod, [delegateDataStruct]);
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            TypeDefinition marshaller = _importer.CreateNewClass(assembly, type.Namespace, type.Name + "Marshaller", TypeAttributes.Class | TypeAttributes.Public);
            
            // Create a delegate from the marshaller
            MethodDefinition fromNativeMethod = marshaller.AddFromNativeMethod(type, importer);
            MethodDefinition toNativeMethod = marshaller.AddToNativeMethod(type, importer);
            ILProcessor processor = fromNativeMethod.Body.GetILProcessor();
            
            MethodReference constructor = type.FindMethod(_importer, ".ctor", true, delegateDataStruct)!;
            constructor.DeclaringType = type;

            VariableDefinition delegateDataVar = fromNativeMethod.AddLocalVariable(delegateDataStruct);
            
            // Load native buffer
            processor.Emit(OpCodes.Ldarg_0);
            
            // Load array offset of 0
            processor.Emit(OpCodes.Ldc_I4_0);
            
            processor.Emit(OpCodes.Call, blittablefromNativeMethod);
            processor.Emit(OpCodes.Stloc, delegateDataVar);
            
            processor.Emit(OpCodes.Ldloc, delegateDataVar);
            
            MethodReference? constructorDelegate = type.FindMethod(_importer, ".ctor", true, [delegateDataStruct]);
            processor.Emit(OpCodes.Newobj, constructorDelegate);
            processor.Emit(OpCodes.Ret);
            
            MethodReference? invokerMethod = type.FindMethod(_importer, "Invoker");
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate type");
            }
            
            FunctionMetaData functionMetaData = new FunctionMetaData(_importer, invokerMethod.Resolve());
            DelegateMetaData newDelegate = new DelegateMetaData(_importer, functionMetaData, 
                type, 
                "USingleDelegate");
            delegateMetaData.Add(newDelegate);
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            WriteInvokerMethod(type, invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }

    public void WriteInvokerMethod(TypeDefinition delegateType, MethodReference invokerMethod, FunctionMetaData functionMetaData)
    {
        GenericInstanceType baseGenericDelegateType = (GenericInstanceType)delegateType.BaseType;
        TypeReference processDelegateType = baseGenericDelegateType.GenericArguments[0];
        
        MethodReference processDelegateBase = delegateType.FindMethod(_importer, "ProcessDelegate")!;


        MethodReference declaredType = _importer.ImportMethod(_importer.FunctionProcessor.MakeMethodDeclaringTypeGeneric(processDelegateBase.Resolve().GetBaseMethod(),
                _importer.ImportType(processDelegateType)));
        
        MethodDefinition invokerMethodDefinition = invokerMethod.Resolve();
        ILProcessor invokerMethodProcessor = invokerMethodDefinition.Body.GetILProcessor();
        invokerMethodProcessor.Body.Instructions.Clear();

        if (functionMetaData.Parameters.Length > 0)
        {
            functionMetaData.FunctionPointerField = delegateType.AddField("SignatureFunction", _importer.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
            List<Instruction> allCleanupInstructions = [];

            for (int i = 0; i < functionMetaData.Parameters.Length; ++i)
            {
                PropertyMetaData param = functionMetaData.Parameters[i];
                NativeDataType nativeDataType = param.PropertyDataType;
                
                if (param.MemberRef == null)
                {
                    throw new Exception($"Parameter {param.Name} does not have a valid member reference");
                }

                nativeDataType.PrepareForRewrite(invokerMethodDefinition.DeclaringType, param, param.MemberRef);
            }

            _functionProcessor.WriteParametersToNative(invokerMethodProcessor,
                invokerMethodDefinition,
                functionMetaData,
                functionMetaData.RewriteInfo.FunctionParamSizeField,
                functionMetaData.RewriteInfo.FunctionParams,
                out var loadArguments,
                out _, allCleanupInstructions);

            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldloc, loadArguments);
        }
        else
        {
            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldsfld, _importer.IntPtrZero);
        }
        
        invokerMethodProcessor.Emit(OpCodes.Callvirt, declaredType);
        invokerMethodDefinition.FinalizeMethod();
    }

    public MethodReference FindOrCreateInitializeDelegate(TypeDefinition delegateType)
    {
        MethodReference? initializeDelegate = delegateType.FindMethod(_importer, InitializeUnrealDelegate, false);
        
        if (initializeDelegate == null)
        {
            initializeDelegate = delegateType.AddMethod(_importer, InitializeUnrealDelegate,
                _importer.VoidTypeRef, MethodAttributes.Public | MethodAttributes.Static, _importer.IntPtrType);
        }
        
        return _importer.ImportMethod(initializeDelegate);
    }
    
    void ProcessInitialize(TypeDefinition type, FunctionMetaData functionMetaData)
    {
        MethodDefinition initializeMethod = FindOrCreateInitializeDelegate(type).Resolve();
        ILProcessor? processor = initializeMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, importer.GetSignatureFunction);
        processor.Emit(OpCodes.Stsfld, functionMetaData.FunctionPointerField);
        
        Instruction loadFunctionPointer = processor.Create(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        functionMetaData.EmitFunctionParamOffsets(processor, loadFunctionPointer);
        functionMetaData.EmitFunctionParamSize(processor, loadFunctionPointer);
        functionMetaData.EmitParamNativeProperty(processor, loadFunctionPointer);
        
        initializeMethod.FinalizeMethod();
    }
}