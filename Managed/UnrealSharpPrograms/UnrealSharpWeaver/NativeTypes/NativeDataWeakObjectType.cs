using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;
class NativeDataWeakObjectType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeRef, int arrayDim) 
    : NativeDataGenericObjectType(importer, typeRef, innerTypeRef, "BlittableMarshaller`1", arrayDim, PropertyType.WeakObject);