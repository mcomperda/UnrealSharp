namespace UnrealSharpBuildTool;

public static class BuildToolConstants
{
    public const string UserProjectFolderName = "Script";
    public const string PluginsFolder = "Plugins";
    public const string UnrealSharpPluginName = "UnrealSharp";
    public const string SourceGeneratorsAssemblyFileName = "UnrealSharp.SourceGenerators.dll";
    public const string GlueProjectName = "ProjectGlue";
    public const string GlueProjectFilename = $"{GlueProjectName}.csproj";
    

    public static readonly string ManagedBinariesFolderPath = Path.Combine("Binaries", "Managed");                
    public static readonly string UnrealSharpPluginsFolder = Path.Combine(PluginsFolder, UnrealSharpPluginName);
    public static readonly string UnrealSharpBindingsSolutionDirectory = Path.Combine(UnrealSharpPluginsFolder, "Managed", UnrealSharpPluginName);

    public static readonly string UnrealEditorFileName;
    public static readonly string UnrealEditorFilePath;

    static BuildToolConstants()
    {
        if (OperatingSystem.IsWindows())
        {
            UnrealEditorFileName = "UnrealEditor.exe";
            UnrealEditorFilePath = Path.Combine("Binaries", "Win64", UnrealEditorFileName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            UnrealEditorFileName = "UnrealEditor";
            UnrealEditorFilePath = Path.Combine("Binaries", "Mac", UnrealEditorFileName);
        }            
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system for UnrealSharpBuildTool.");
        }

    }
}
