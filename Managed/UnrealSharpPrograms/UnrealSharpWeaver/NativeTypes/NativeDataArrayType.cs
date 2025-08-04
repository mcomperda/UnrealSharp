using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataArrayType(WeaverImporter importer, TypeReference typeRef, int containerDim, TypeReference innerType) 
    : NativeDataContainerType(importer, typeRef, containerDim, PropertyType.Array, innerType)
{
    public override string GetContainerMarshallerName()
    {
        return "ArrayMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "ArrayCopyMarshaller`1";
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.IList`1";
    }
}