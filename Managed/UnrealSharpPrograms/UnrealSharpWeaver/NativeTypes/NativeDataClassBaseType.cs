using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassBaseType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeReference, int arrayDim, string marshallerClass, PropertyType propertyType)
    : NativeDataGenericObjectType(importer, typeRef, innerTypeReference, marshallerClass, arrayDim, propertyType)
{
    protected override TypeReference[] GetTypeParams()
    {
        return [_importer.ImportType(InnerType.TypeRef)];
    }
};