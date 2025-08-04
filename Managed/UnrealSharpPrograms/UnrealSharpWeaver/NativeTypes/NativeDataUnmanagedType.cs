using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataUnmanagedType(WeaverImporter importer, TypeReference unmanagedType, int arrayDim) : NativeDataSimpleType(importer, unmanagedType, "UnmanagedTypeMarshaller`1", arrayDim, PropertyType.Struct)
{
    public TypeReferenceMetadata InnerType { get; set; } =  new(importer,importer.UnmanagedDataStore.Resolve());
}