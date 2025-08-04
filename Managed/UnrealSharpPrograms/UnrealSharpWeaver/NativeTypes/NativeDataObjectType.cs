using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataObjectType(WeaverImporter importer, TypeReference propertyTypeRef, TypeReference innerTypeReference, int arrayDim) 
    : NativeDataGenericObjectType(importer, propertyTypeRef, innerTypeReference, "ObjectMarshaller`1", arrayDim, PropertyType.Object);
