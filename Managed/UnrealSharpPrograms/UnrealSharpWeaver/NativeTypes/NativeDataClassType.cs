using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataClassBaseType(importer, typeRef, innerTypeReference, arrayDim, "SubclassOfMarshaller`1", PropertyType.Class);