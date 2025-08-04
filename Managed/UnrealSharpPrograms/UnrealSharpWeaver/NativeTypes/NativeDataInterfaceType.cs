using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataInterfaceType : NativeDataSimpleType
{
    public NativeDataInterfaceType(WeaverImporter importer, TypeReference typeRef, string marshallerName) : base(importer, typeRef, marshallerName, 0, PropertyType.ScriptInterface)
    {
        InnerType = new TypeReferenceMetadata(importer, typeRef.Resolve());
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata, object outer)
    {
        TypeDefinition interfaceTypeDef = InnerType.TypeRef.Resolve();
        if (!interfaceTypeDef.IsUInterface())
        {
            throw new Exception($"{interfaceTypeDef.FullName} needs to be a UInterface if exposed to Unreal Engine's reflection system!");
        }
        
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
    }

    public TypeReferenceMetadata InnerType { get; set; }
}