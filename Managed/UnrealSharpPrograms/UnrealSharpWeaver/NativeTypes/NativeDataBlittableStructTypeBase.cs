using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructTypeBase(WeaverImporter importer, TypeReference structType, int arrayDim, PropertyType propertyType = PropertyType.Struct)
    : NativeDataStructType(importer, structType, "BlittableMarshaller`1", arrayDim, propertyType)
{
    public override bool IsBlittable => true;
}