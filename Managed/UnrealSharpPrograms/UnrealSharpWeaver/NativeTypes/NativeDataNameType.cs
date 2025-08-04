using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataNameType(WeaverImporter importer, TypeReference structType, int arrayDim) : NativeDataBlittableStructTypeBase(importer, structType, arrayDim, PropertyType.Name);