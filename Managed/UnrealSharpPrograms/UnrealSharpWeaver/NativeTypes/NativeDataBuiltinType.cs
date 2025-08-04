using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBuiltinType(WeaverImporter importer, TypeReference typeRef, int arrayDim, PropertyType propertyType) : NativeDataSimpleType(importer, typeRef, "BlittableMarshaller`1", arrayDim, propertyType)
{ 
    public override bool IsBlittable => true;
}
