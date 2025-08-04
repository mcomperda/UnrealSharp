using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public class UnrealInterfaceProcessor(WeaverImporter importer)
{
    private readonly WeaverImporter _importer = importer;
    private readonly ConstructorBuilder _constructorBuilder = new(importer);
    private readonly FunctionProcessor _functionProcessor = new(importer);

    public void ProcessInterfaces(List<TypeDefinition> interfaces, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.InterfacesMetaData.Capacity = interfaces.Count;
        
        for (var i = 0; i < interfaces.Count; ++i)
        {
            TypeDefinition interfaceType = interfaces[i];
            assemblyMetadata.InterfacesMetaData.Add(new InterfaceMetaData(_importer, interfaceType));            
            CreateInterfaceMarshaller(interfaceType);
            _importer.Logger.Info($"Processed interface '{interfaceType.FullName}'");
        }
    }

    public void CreateInterfaceMarshaller(TypeDefinition interfaceType)
    {
        TypeDefinition structMarshallerClass = _importer.CreateNewClass(interfaceType.Namespace, interfaceType.GetMarshallerClassName(), 
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        
        FieldDefinition nativePointerField = structMarshallerClass.AddField("NativeInterfaceClassPtr", 
            _importer.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
        
        string interfaceName = interfaceType.GetEngineName();
        const bool finalizeMethod = true;

        _constructorBuilder.CreateTypeInitializer(structMarshallerClass, Instruction.Create(OpCodes.Stsfld, nativePointerField), 
            [Instruction.Create(OpCodes.Call, _importer.GetNativeClassFromNameMethod)], interfaceName, finalizeMethod);
        
        MakeToNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
        MakeFromNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
    }
    
    public void MakeToNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition toNativeMarshallerMethod = interfaceType.AddMethod(_importer, "ToNative", 
            _importer.VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static, _importer.IntPtrType, _importer.Int32TypeRef, interfaceType);
        
        MethodReference toNativeMethod = _importer.ScriptInterfaceMarshaller.FindMethod(_importer, "ToNative")!;
        toNativeMethod = _functionProcessor.MakeMethodDeclaringTypeGeneric(toNativeMethod, interfaceType);
        
        ILProcessor toNativeMarshallerProcessor = toNativeMarshallerMethod.Body.GetILProcessor();
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_2);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldsfld, nativePointerField);
        toNativeMarshallerProcessor.Emit(OpCodes.Call, toNativeMethod);
        
        toNativeMarshallerMethod.FinalizeMethod();
    }
    
    public void MakeFromNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition fromNativeMarshallerMethod = structMarshallerClass.AddMethod(_importer, "FromNative", 
            interfaceType,
            MethodAttributes.Public | MethodAttributes.Static,
            [_importer.IntPtrType, _importer.Int32TypeRef]);
        
        MethodReference fromNativeMethod = _importer.ScriptInterfaceMarshaller.FindMethod(_importer, "FromNative")!;
        fromNativeMethod = _functionProcessor.MakeMethodDeclaringTypeGeneric(fromNativeMethod, interfaceType);
        
        ILProcessor fromNativeMarshallerProcessor = fromNativeMarshallerMethod.Body.GetILProcessor();
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        fromNativeMarshallerProcessor.Emit(OpCodes.Call, fromNativeMethod);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ret);
        fromNativeMarshallerMethod.OptimizeMethod();
    }
}