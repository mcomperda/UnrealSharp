using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftObjectType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataClassBaseType(importer, typeRef, innerTypeReference, arrayDim, "SoftObjectMarshaller`1", PropertyType.SoftObject);