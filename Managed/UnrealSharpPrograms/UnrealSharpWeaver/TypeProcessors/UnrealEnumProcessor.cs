using LanguageExt;
using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public class UnrealEnumProcessor(WeaverImporter importer) : BaseProcessor(importer)
{ 
    public void ProcessEnums(List<TypeDefinition> foundEnums, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.EnumMetaData.Capacity = foundEnums.Count;
        
        for (var i = 0; i < foundEnums.Count; i++)
        {
            var found = foundEnums[i];
            assemblyMetadata.EnumMetaData.Add(new EnumMetaData(_importer, found));
            _importer.Logger.Info($"Processed enum '{found.FullName}'");
        }
    }
}