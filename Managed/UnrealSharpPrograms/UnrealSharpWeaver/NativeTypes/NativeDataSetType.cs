using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataSetType : NativeDataContainerType
{
    public NativeDataSetType(WeaverImporter importer, TypeReference typeRef, int containerDim, TypeReference value) 
        : base(importer, typeRef, containerDim, PropertyType.Set, value)
    {
    }
    
    public override string GetContainerMarshallerName()
    {
        return "SetMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "SetCopyMarshaller`1";
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.ISet`1";
    }
}