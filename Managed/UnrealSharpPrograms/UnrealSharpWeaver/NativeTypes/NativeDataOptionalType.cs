using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

internal class NativeDataOptionalType(WeaverImporter importer, TypeReference propertyTypeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataContainerType(importer, propertyTypeRef, arrayDim, PropertyType.Optional, innerTypeReference)
    {
        
        protected override AssemblyDefinition MarshallerAssembly => _importer.UnrealSharpCoreAssembly;
        protected override string MarshallerNamespace => WeaverImporter.UnrealSharpCoreMarshallers;
        
    public override string GetContainerMarshallerName()
    {
        return "OptionMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "OptionMarshaller`1";
    }
}