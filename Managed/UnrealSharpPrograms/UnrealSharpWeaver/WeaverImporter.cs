using Mono.Cecil;
using Mono.Cecil.Rocks;
using UnrealSharp.Tools;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public partial class WeaverImporter
{    
    private const string Attributes = ".Attributes";

    public const string UnrealSharpNamespace = "UnrealSharp";
    public const string UnrealSharpAttributesNamespace = UnrealSharpNamespace + Attributes;

    public const string UnrealSharpCoreNamespace = UnrealSharpNamespace + ".Core";
    public const string UnrealSharpCoreAttributesNamespace = UnrealSharpCoreNamespace + Attributes;
    public const string UnrealSharpCoreMarshallers = UnrealSharpCoreNamespace + ".Marshallers";

    public const string InteropNameSpace = UnrealSharpNamespace + ".Interop";
    public const string AttributeNamespace = UnrealSharpNamespace + Attributes;
    public const string CoreUObjectNamespace = UnrealSharpNamespace + ".CoreUObject";
    public const string EngineNamespace = UnrealSharpNamespace + ".Engine";

    public const string UnrealSharpObject = "UnrealSharpObject";
    public const string FPropertyCallbacks = "FPropertyExporter";

    public const string CoreUObjectCallbacks = "UCoreUObjectExporter";
    public const string UObjectCallbacks = "UObjectExporter";
    public const string UScriptStructCallbacks = "UScriptStructExporter";
    public const string UFunctionCallbacks = "UFunctionExporter";
    public const string MulticastDelegatePropertyCallbacks = "FMulticastDelegatePropertyExporter";
    public const string UStructCallbacks = "UStructExporter";

    public const string GeneratedTypeAttribute = "GeneratedTypeAttribute";
        
    public MethodReference? UFunctionAttributeConstructor => FindType(UnrealSharpAssembly, "UFunctionAttribute", "UnrealSharp.Attributes")?.FindMethod(this, ".ctor");
    public MethodReference? BlueprintInternalUseAttributeConstructor => FindType(UnrealSharpAssembly, "BlueprintInternalUseOnlyAttribute", "UnrealSharp.Attributes.MetaTags")?.FindMethod(this, ".ctor");
    
    public AssemblyDefinition UserAssembly = null!;
    public readonly ICollection<AssemblyDefinition> WeavedAssemblies = [];

    public WeaverOptions Options { get; }
    public ToolLogger Logger { get; }

    public UnrealEnumProcessor UnrealEnumProcessor { get; }
    public UnrealInterfaceProcessor UnrealInterfaceProcessor { get; }
    public UnrealStructProcessor UnrealStructProcessor { get; }
    public UnrealClassProcessor UnrealClassProcessor { get; }
    public UnrealDelegateProcessor UnrealDelegateProcessor { get; }
    public FunctionProcessor FunctionProcessor { get; }
    public ConstructorBuilder ConstructorBuilder { get; }
    public PropertyProcessor PropertyProcessor { get; }

    public WeaverImporter(WeaverOptions options, ToolLogger logger)
    {
        Options = options;
        Logger = logger;

        UnrealEnumProcessor = new(this);
        UnrealInterfaceProcessor = new(this);
        UnrealStructProcessor = new(this);
        UnrealClassProcessor = new(this);
        UnrealDelegateProcessor = new(this);
        FunctionProcessor = new(this);
        ConstructorBuilder = new(this);
        PropertyProcessor = new(this);
    }

    public AssemblyDefinition UnrealSharpAssembly => FindAssembly(UnrealSharpNamespace);
    public AssemblyDefinition UnrealSharpCoreAssembly => FindAssembly(UnrealSharpNamespace + ".Core");
    public AssemblyDefinition ProjectGlueAssembly => FindAssembly("ProjectGlue");
    
    public MethodReference NativeObjectGetter = null!;
    public TypeDefinition IntPtrType = null!;
    public MethodReference IntPtrAdd = null!;
    public FieldReference IntPtrZero = null!;
    public MethodReference IntPtrEqualsOperator = null!;
    public TypeReference UnrealSharpObjectType = null!;
    public TypeDefinition IInterfaceType = null!;
    public MethodReference GetNativeFunctionFromInstanceAndNameMethod = null!;
    public TypeReference Int32TypeRef = null!;
    public TypeReference UInt64TypeRef = null!;
    public TypeReference VoidTypeRef = null!;
    public TypeReference ByteTypeRef = null!;
    public MethodReference GetNativeClassFromNameMethod = null!;
    public MethodReference GetNativeStructFromNameMethod = null!;
    public MethodReference GetPropertyOffsetFromNameMethod = null!;
    public MethodReference GetPropertyOffset = null!;
    public MethodReference GetNativePropertyFromNameMethod = null!;
    public MethodReference GetNativeFunctionFromClassAndNameMethod = null!;
    public MethodReference GetNativeFunctionParamsSizeMethod = null!;
    public MethodReference GetNativeStructSizeMethod = null!;
    public MethodReference GetSignatureFunction = null!;
    public MethodReference InitializeStructMethod = null!;
    
    public MethodReference InvokeNativeFunctionMethod = null!;
    public MethodReference InvokeNativeNetFunction = null!;
    public MethodReference InvokeNativeFunctionOutParms = null!;

    public MethodReference GeneratedTypeCtor = null!;
    
    public TypeDefinition UObjectDefinition = null!;
    public TypeDefinition UActorComponentDefinition = null!;
    
    public TypeDefinition ScriptInterfaceMarshaller = null!;
    public TypeReference ManagedObjectHandle = null!;
    public TypeReference UnmanagedDataStore = null!;
    
    public MethodReference BlittableTypeConstructor = null!;

    private readonly DefaultAssemblyResolver _assemblyResolver = new();
    public DefaultAssemblyResolver AssemblyResolver => _assemblyResolver;
    

    AssemblyDefinition FindAssembly(string assemblyName)
    {
        return _assemblyResolver.Resolve(new AssemblyNameReference(assemblyName, new Version(0, 0)));
    }

    public void ImportCommonTypes(AssemblyDefinition userAssembly)
    {
        UserAssembly = userAssembly;
        
        TypeSystem typeSystem = UserAssembly.MainModule.TypeSystem;
        
        Int32TypeRef = typeSystem.Int32;
        UInt64TypeRef = typeSystem.UInt64;
        VoidTypeRef = typeSystem.Void;
        ByteTypeRef = typeSystem.Byte;
        
        IntPtrType = typeSystem.IntPtr.Resolve();
        IntPtrAdd = IntPtrType.FindMethod(this, "Add")!;
        IntPtrZero = IntPtrType.FindField(this, "Zero");
        IntPtrEqualsOperator = IntPtrType.FindMethod(this,"op_Equality")!;

        UnrealSharpObjectType = FindType(UnrealSharpCoreAssembly, UnrealSharpObject, UnrealSharpCoreNamespace)!;
        IInterfaceType = FindType(UnrealSharpAssembly, "IInterface", CoreUObjectNamespace)!.Resolve();
        
        TypeDefinition unrealSharpObjectType = UnrealSharpObjectType.Resolve();
        NativeObjectGetter = unrealSharpObjectType.FindMethod(this,"get_NativeObject")!;

        GetNativeFunctionFromInstanceAndNameMethod = FindExporterMethod(TypeDefinitionUtilities.UClassCallbacks, "CallGetNativeFunctionFromInstanceAndName");
        
        GetNativeStructFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeStructFromName");
        GetNativeClassFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeClassFromName");
        
        GetPropertyOffsetFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffsetFromName");
        GetPropertyOffset = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffset");
        
        GetNativePropertyFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetNativePropertyFromName");
        
        GetNativeFunctionFromClassAndNameMethod = FindExporterMethod(TypeDefinitionUtilities.UClassCallbacks, "CallGetNativeFunctionFromClassAndName");
        GetNativeFunctionParamsSizeMethod = FindExporterMethod(UFunctionCallbacks, "CallGetNativeFunctionParamsSize");
        
        GetNativeStructSizeMethod = FindExporterMethod(UScriptStructCallbacks, "CallGetNativeStructSize");
        
        InvokeNativeFunctionMethod = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeFunction");
        InvokeNativeNetFunction = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeNetFunction");
        InvokeNativeFunctionOutParms = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeFunctionOutParms");
        
        GetSignatureFunction = FindExporterMethod(MulticastDelegatePropertyCallbacks, "CallGetSignatureFunction");
        
        InitializeStructMethod = FindExporterMethod(UStructCallbacks, "CallInitializeStruct");
        
        UObjectDefinition = FindType(UnrealSharpAssembly, "UObject", CoreUObjectNamespace)!.Resolve();
        UActorComponentDefinition = FindType(UnrealSharpAssembly, "UActorComponent", EngineNamespace)!.Resolve();
        
        TypeReference blittableType = FindType(UnrealSharpCoreAssembly, TypeDefinitionUtilities.BlittableTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        BlittableTypeConstructor = blittableType.FindMethod(this, ".ctor")!;

        TypeReference generatedType = FindType(UnrealSharpCoreAssembly, GeneratedTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        GeneratedTypeCtor = generatedType.FindMethod(this,".ctor")!;
        
        ScriptInterfaceMarshaller = FindType(UnrealSharpAssembly, "ScriptInterfaceMarshaller`1", CoreUObjectNamespace)!.Resolve();
        
        ManagedObjectHandle = FindType(UnrealSharpAssembly, "FSharedGCHandle", "UnrealSharp.UnrealSharpCore")!.Resolve();
        UnmanagedDataStore = FindType(UnrealSharpAssembly, "FUnmanagedDataStore", "UnrealSharp.UnrealSharpCore")!.Resolve();
    }

    private MethodReference FindBindingsStaticMethod(string findNamespace, string findClass, string findMethod)
    {
        foreach (var module in UnrealSharpAssembly.Modules)
        {
            foreach (var type in module.GetAllTypes())
            {
                if (type.Namespace != findNamespace || type.Name != findClass)
                {
                    continue;
                }

                foreach (var method in type.Methods)
                {
                    if (method.IsStatic && method.Name == findMethod)
                    {
                        return UserAssembly.MainModule.ImportReference(method);
                    }
                }
            }
        }
        
        throw new Exception("Could not find method " + findMethod + " in class " + findClass + " in namespace " + findNamespace);
    }

    private MethodReference FindExporterMethod(string exporterName, string functionName)
    {
        return FindBindingsStaticMethod(InteropNameSpace, exporterName, functionName);
    }
}
