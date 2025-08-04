using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public abstract class NativeDataBaseDelegateType : NativeDataSimpleType
{
    public TypeReferenceMetadata UnrealDelegateType { get; set; }

    public MethodDefinition Signature;
    protected TypeReference fieldType;
    protected TypeReference delegateType;
    protected TypeReference wrapperType;

    protected override TypeReference[] GetTypeParams()
    {
        return [_importer.ImportType(delegateType)];
    }

    public NativeDataBaseDelegateType(WeaverImporter importer, TypeReference typeRef, string marshallerName, PropertyType propertyType)
        : base(importer, typeRef, marshallerName, 1, propertyType)
    {

        fieldType = typeRef;
        delegateType = GetDelegateType(typeRef);
        wrapperType = GetWrapperType(delegateType);

        UnrealDelegateType = new TypeReferenceMetadata(_importer, wrapperType);
        UnrealDelegateType.Name = DelegateUtilities.GetUnrealDelegateName(wrapperType);

        TypeDefinition delegateTypeDefinition = delegateType.Resolve();
        foreach (MethodDefinition method in delegateTypeDefinition.Methods)
        {
            if (method.Name != "Invoke")
            {
                continue;
            }

            if (!method.ReturnsVoid())
            {
                throw new Exception($"{delegateType.FullName} is exposed to Unreal Engine, and must have a void return type.");
            }

            Signature = method;
            return;
        }

        if (Signature == null)
        {
            throw new Exception("Could not find Invoke method in delegate type");
        }
    }

    protected TypeReference GetDelegateType(TypeReference typeRef)
    {
        return ((GenericInstanceType)typeRef).GenericArguments[0];
    }

    protected TypeReference GetWrapperType(TypeReference delegateType)
    {
        TypeDefinition delegateTypeDefinition = delegateType.Resolve();
        var assembly = delegateTypeDefinition.Module.Assembly;

        return _importer.FindType(assembly, "U{delegateType.Name}", delegateType.Namespace)!;
    }

}