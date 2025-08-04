using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBooleanType(WeaverImporter importer, TypeReference typeRef, int arrayDim) : NativeDataSimpleType(importer, typeRef, "BoolMarshaller", arrayDim, PropertyType.Bool)
{
    public override bool IsPlainOldData => false;
}