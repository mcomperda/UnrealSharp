using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructType(WeaverImporter importer, TypeReference structType, int arrayDim) : NativeDataBlittableStructTypeBase(importer, structType, arrayDim);