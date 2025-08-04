using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace UnrealSharpWeaver.TypeProcessors;

public class UnrealStructProcessor(WeaverImporter importer)
{
    private readonly WeaverImporter _importer = importer;
    private readonly ConstructorBuilder _constructorBuilder = new(importer);
    private readonly FunctionProcessor _functionProcessor = new(importer);

    public void ProcessStructs(IEnumerable<TypeDefinition> structs, ApiMetaData assemblyMetadata, AssemblyDefinition assembly)
    {
        // We need to create struct metadata in the correct order to ensure that blittable structs have
        // their UStruct attributes updated before other referencing structs use them to create UnrealTypes.
        var structStack = new Stack<TypeDefinition>();
        var pushedStructs = new HashSet<TypeDefinition>();
        var structHandlingOrder = new List<TypeDefinition>();
        var structMetadata = new Dictionary<TypeDefinition, StructMetaData>();

        var sortedStructs = structs.ToList();
        sortedStructs.Sort((a, b) =>
        {
            var aMetadata = new StructMetaData(_importer, a);
            var bMetadata = new StructMetaData(_importer, b);

            foreach (var Field in aMetadata.Fields)
            {
                if (Field.PropertyDataType.CSharpType.FullName.Contains(bMetadata.TypeRef.FullName))
                {
                    return 1;
                }
            }

            foreach (var Field in bMetadata.Fields)
            {
                if (Field.PropertyDataType.CSharpType.FullName.Contains(aMetadata.TypeRef.Name))
                {
                    return -1;
                }
            }
            return 0;
        });

        foreach (var unrealStruct in sortedStructs.Where(unrealStruct => !pushedStructs.Contains(unrealStruct)))
        {
            structStack.Push(unrealStruct);
            pushedStructs.Add(unrealStruct);

            PushReferencedStructsFromAssembly(assembly, unrealStruct, structStack, pushedStructs);

            while (structStack.Count > 0) 
            {
                var currentStruct = structStack.Pop();
                try 
                {
                    if (structMetadata.ContainsKey(currentStruct)) 
                    {
                        throw new RewriteException (currentStruct, "Attempted to create struct metadata twice");
                    }
                    
                    var currentMetadata = new StructMetaData(_importer, currentStruct);
                    structHandlingOrder.Add(currentStruct);
                    structMetadata.Add(currentStruct, currentMetadata);
                } 
                catch (WeaverProcessError error) 
                {
                    ErrorEmitter.Error (error);
                }
            }
        }
        assemblyMetadata.StructMetaData = structMetadata.Values.ToList();
        
        foreach (var currentStruct in structHandlingOrder)
        {
            ProcessStruct(currentStruct, structMetadata[currentStruct]);
            _importer.Logger.Info($"Processed struct '{currentStruct.FullName}'");
        }
    }
    
    private void ProcessStruct(TypeDefinition structTypeDefinition, StructMetaData metadata)
    {
        MethodReference? foundConstructor = structTypeDefinition.FindMethod(_importer, ".ctor", false, _importer.IntPtrType);
        
        if (foundConstructor != null)
        {
            throw new RewriteException(structTypeDefinition, "Structs cannot have a constructor that takes an IntPtr");
        }
        
        var propertyOffsetsToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        var propertyPointersToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        _importer.PropertyProcessor.ProcessClassMembers(ref propertyOffsetsToInitialize, ref propertyPointersToInitialize, structTypeDefinition, metadata.Fields);

        MethodDefinition structConstructor = _constructorBuilder.CreateConstructor(structTypeDefinition, MethodAttributes.Public, _importer.IntPtrType);
        var toNativeMethod = _functionProcessor.CreateMethod(structTypeDefinition, "ToNative", MethodAttributes.Public, null, [_importer.IntPtrType]);
        
        ILProcessor constructorBody = structConstructor.Body.GetILProcessor();
        ILProcessor toNativeBody = toNativeMethod.Body.GetILProcessor();
        Instruction loadBufferInstruction = Instruction.Create(OpCodes.Ldarg_1);
        
        foreach (PropertyMetaData prop in metadata.Fields)
        {
            if (prop.MemberRef == null)
            {
                throw new InvalidDataException($"Property '{prop.Name}' does not have a member reference");
            }
            
            if (prop.PropertyOffsetField == null)
            {
                throw new InvalidDataException($"Property '{prop.Name}' does not have an offset field");
            }
            
            FieldDefinition fieldDefinition = (FieldDefinition) prop.MemberRef.Resolve();
            prop.PropertyDataType.WriteLoad(constructorBody, structTypeDefinition, loadBufferInstruction, prop.PropertyOffsetField, fieldDefinition);
            prop.PropertyDataType.WriteStore(toNativeBody, structTypeDefinition, loadBufferInstruction, prop.PropertyOffsetField, fieldDefinition);
        }
        
        structConstructor.FinalizeMethod();
        toNativeMethod.FinalizeMethod();
        
        // Field to cache the native size of the struct.
        FieldDefinition nativeStructSizeField = structTypeDefinition.AddField("NativeDataSize", _importer.Int32TypeRef, FieldAttributes.Public | FieldAttributes.Static);
        Instruction callGetNativeStructFromNameMethod = Instruction.Create(OpCodes.Call, _importer.GetNativeStructFromNameMethod);
        Instruction callGetNativeStructSizeMethod = Instruction.Create(OpCodes.Call, _importer.GetNativeStructSizeMethod);
        Instruction setNativeStructSizeField = Instruction.Create(OpCodes.Stsfld, nativeStructSizeField);
        _constructorBuilder.CreateTypeInitializer(structTypeDefinition, setNativeStructSizeField, [callGetNativeStructFromNameMethod, callGetNativeStructSizeMethod]);
        
        CreateStructMarshaller(structTypeDefinition, nativeStructSizeField, toNativeMethod, structConstructor);
        CreateStructStaticConstructor(metadata, structTypeDefinition);
    }

    private void CreateStructStaticConstructor(StructMetaData metadata, TypeDefinition structTypeDefinition)
    {
        MethodDefinition staticConstructor = _constructorBuilder.MakeStaticConstructor(structTypeDefinition);
        
        // Create a field to cache the native struct class.
        // nint a = UCoreUObjectExporter.CallGetNativeStructFromName("MyStruct");
        VariableDefinition nativeStructClass = staticConstructor.AddLocalVariable(_importer.IntPtrType);
        Instruction callGetNativeStructFromNameMethod = Instruction.Create(OpCodes.Call, _importer.GetNativeStructFromNameMethod);
        Instruction setNativeStruct = Instruction.Create(OpCodes.Stloc, nativeStructClass);
        _constructorBuilder.CreateTypeInitializer(structTypeDefinition, setNativeStruct, [callGetNativeStructFromNameMethod]);

        _constructorBuilder.InitializeFields(staticConstructor, [.. metadata.Fields], Instruction.Create(OpCodes.Ldloc, nativeStructClass));
        staticConstructor.FinalizeMethod();
    }

    private void CreateStructMarshaller(TypeDefinition structTypeDefinition, FieldDefinition nativeStructSizeField, MethodDefinition toNativeMethod, MethodDefinition structConstructor)
    {
        // Create a marshaller class for the struct.
        TypeDefinition structMarshallerClass = _importer.CreateNewClass(structTypeDefinition.Namespace, structTypeDefinition.GetMarshallerClassName(), 
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
            
        AddToNativeMarshallingMethod(structMarshallerClass, structTypeDefinition, nativeStructSizeField, toNativeMethod);
        AddFromNativeMarshallingMethod(structMarshallerClass, structTypeDefinition, structConstructor, nativeStructSizeField);
    }
    
    private void AddToNativeMarshallingMethod(TypeDefinition marshaller, TypeDefinition structTypeDefinition, FieldDefinition nativeDataSizeField, MethodDefinition toNativeMethod)
    {
        MethodDefinition toNativeMarshallerMethod = marshaller.AddMethod(_importer, "ToNative", 
            _importer.VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static,
            [_importer.IntPtrType, _importer.Int32TypeRef, structTypeDefinition]);
        
        ILProcessor toNativeMarshallerBody = toNativeMarshallerMethod.Body.GetILProcessor();
        toNativeMarshallerBody.Emit(OpCodes.Ldarga, toNativeMarshallerMethod.Parameters[2]);

        toNativeMarshallerBody.Emit(OpCodes.Ldarg_0);

        toNativeMarshallerBody.Emit(OpCodes.Ldarg_1);
        toNativeMarshallerBody.Emit(OpCodes.Ldsfld, nativeDataSizeField);
        toNativeMarshallerBody.Emit(OpCodes.Mul);

        toNativeMarshallerBody.Emit(OpCodes.Call, _importer.IntPtrAdd);

        toNativeMarshallerBody.Emit(OpCodes.Call, toNativeMethod);
        
        toNativeMarshallerMethod.FinalizeMethod();
    }

    //Create a marshaller method to Native code with signature:
    //public static <StructureType> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealObject owner)
    private void AddFromNativeMarshallingMethod(TypeDefinition marshaller, TypeDefinition structTypeDefinition, MethodDefinition ctor, FieldDefinition nativeDataSizeField)
    {
        MethodDefinition fromNativeMarshallerMethod = marshaller.AddMethod(_importer, "FromNative", 
            structTypeDefinition,
            MethodAttributes.Public | MethodAttributes.Static, 
            [_importer.IntPtrType, _importer.Int32TypeRef]);

        ILProcessor fromNativeMarshallerBody = fromNativeMarshallerMethod.Body.GetILProcessor();
        fromNativeMarshallerBody.Emit(OpCodes.Ldarg_0);
        fromNativeMarshallerBody.Emit(OpCodes.Ldarg_1);
        fromNativeMarshallerBody.Emit(OpCodes.Ldsfld, nativeDataSizeField);
        fromNativeMarshallerBody.Emit(OpCodes.Mul);
        fromNativeMarshallerBody.Emit(OpCodes.Call, _importer.IntPtrAdd);
        fromNativeMarshallerBody.Emit(OpCodes.Newobj, ctor);
        
        fromNativeMarshallerMethod.FinalizeMethod();
    }

    private static void PushReferencedStructsFromAssembly(AssemblyDefinition assembly, TypeDefinition unrealStruct, Stack<TypeDefinition> structStack, HashSet<TypeDefinition> pushedStructs)
    {
        var referencedStructs = new List<TypeDefinition>();
        
        foreach (var field in unrealStruct.Fields) 
        {
            TypeDefinition fieldType = field.FieldType.Resolve();
            
            // if it's not in the same assembly, it will have been processed already
            if (assembly != fieldType.Module.Assembly) 
            {
                continue;
            }

            if (!fieldType.IsValueType || !fieldType.IsUStruct() || pushedStructs.Contains(fieldType))
            {
                continue;
            }
            
            referencedStructs.Add(fieldType);
            structStack.Push(fieldType);
            pushedStructs.Add(fieldType);
        }

        foreach (var referencedStruct in referencedStructs) 
        {
            PushReferencedStructsFromAssembly(assembly, referencedStruct, structStack, pushedStructs);
        }
    }
}
