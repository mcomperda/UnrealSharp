using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataManagedObjectType(WeaverImporter importer, TypeReference managedType, int arrayDim) : NativeDataSimpleType(importer, managedType, "ManagedObjectMarshaller`1", arrayDim, PropertyType.Struct)
{
    public TypeReferenceMetadata InnerType { get; set; } =  new(importer, importer.ManagedObjectHandle.Resolve());
}