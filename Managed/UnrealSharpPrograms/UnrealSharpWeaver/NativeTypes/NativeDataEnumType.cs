using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataEnumType(WeaverImporter importer, TypeReference typeRef, int arrayDim) : NativeDataSimpleType(importer, typeRef, "EnumMarshaller`1", arrayDim, PropertyType.Enum)
{
    public TypeReferenceMetadata InnerProperty { get; set; } = new(importer, typeRef.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition,
        PropertyMetaData propertyMetadata, object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        
        if (!InnerProperty.TypeRef.Resolve().IsUEnum())
        {
            throw new Exception($"{propertyMetadata.MemberRef!.FullName} needs to be a UEnum if exposed through UProperty!");
        }
    }
};