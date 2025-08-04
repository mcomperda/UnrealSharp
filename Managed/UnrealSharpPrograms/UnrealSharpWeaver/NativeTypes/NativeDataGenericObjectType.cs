using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

abstract class NativeDataGenericObjectType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeReference, string marshallerClass, int arrayDim, PropertyType propertyType)
    : NativeDataSimpleType(importer, typeRef, marshallerClass, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(importer, innerTypeReference.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        
        if (!_importer.IsUObject(InnerType.TypeRef.Resolve()))
        {
            throw new Exception($"{propertyMetadata.MemberRef!.FullName} needs to be a UClass if exposed through UProperty!");
        }
    }
}