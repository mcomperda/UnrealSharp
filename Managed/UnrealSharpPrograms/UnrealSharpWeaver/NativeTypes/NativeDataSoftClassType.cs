using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftClassType(WeaverImporter importer, TypeReference typeRef, TypeReference innerTypeReference, int arrayDim) 
    : NativeDataClassBaseType(importer, typeRef, innerTypeReference, arrayDim, "SoftClassMarshaller`1", PropertyType.SoftClass);