using Newtonsoft.Json;
using System.Xml;
using UnrealSharp.Tools;

namespace UnrealSharpBuildTool;

public class BuildToolProjectFileManager
{
    private readonly BuildToolPaths _paths;
    private readonly ToolLogger _logger;
    private readonly DirectoryInfo _projectDirectory;    
    private readonly FileInfo _projectFile;
    private readonly BuildToolOptions _options;

    public BuildToolProjectFileManager(DirectoryInfo projectDirectory , FileInfo projectFile, BuildToolOptions options, BuildToolPaths paths, ToolLogger logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectDirectory = projectDirectory ?? throw new ArgumentNullException(nameof(projectDirectory));
        _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
        _options = options ?? throw new ArgumentNullException(nameof(options));        
    }

    public void WriteProjectFile()
    {
        try
        {
            var projectDocument = new XmlDocument();
            projectDocument.Load(_projectFile.FullName);

            if (projectDocument.SelectSingleNode("//ItemGroup") is not XmlElement newItemGroup)
            {
                newItemGroup = projectDocument.CreateElement("ItemGroup");
                projectDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            AppendProperties(projectDocument);

            AppendPackageReference(projectDocument, newItemGroup, "LanguageExt.Core", "4.4.9");

            var pluginBinaryPath = _paths.GetDevelopmentAssembliesDirectory();
            AppendReference(projectDocument, newItemGroup, "UnrealSharp", pluginBinaryPath.FullName);
            AppendReference(projectDocument, newItemGroup, "UnrealSharp.Core", pluginBinaryPath.FullName);

            AppendSourceGeneratorReference(projectDocument, newItemGroup);

            if (!_options.HasArgument("SkipIncludeProjectGlue"))
            {
                AppendGeneratedCode(projectDocument, newItemGroup);
            }

            projectDocument.Save(_projectFile.FullName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update the .csproj file at {_projectFile.FullName}. Error: {ex.Message}", ex);
        }
    }

    private static void AddProperty(string name, string value, XmlDocument doc, XmlNode propertyGroup)
    {
        XmlNode? newProperty = propertyGroup.SelectSingleNode(name);

        if (newProperty == null)
        {
            newProperty = doc.CreateElement(name);
            propertyGroup.AppendChild(newProperty);
        }

        newProperty.InnerText = value;
    }

    private static void AppendProperties(XmlDocument doc)
    {
        XmlNode? propertyGroup = doc.SelectSingleNode("//PropertyGroup");

        if (propertyGroup == null)
        {
            propertyGroup = doc.CreateElement("PropertyGroup");
        }

        AddProperty("CopyLocalLockFileAssembliesName", "true", doc, propertyGroup);
        AddProperty("AllowUnsafeBlocks", "true", doc, propertyGroup);
        AddProperty("EnableDynamicLoading", "true", doc, propertyGroup);
    }

    private static void AppendPackageReference(XmlDocument doc, XmlElement itemGroup, string packageName, string packageVersion)
    {
        XmlElement packageReference = doc.CreateElement("PackageReference");
        packageReference.SetAttribute("Include", packageName);
        packageReference.SetAttribute("Version", packageVersion);
        itemGroup.AppendChild(packageReference);
    }

    private void AppendReference(XmlDocument doc, XmlElement itemGroup, string assemblyName, string binPath)
    {
        XmlElement referenceElement = doc.CreateElement("Reference");
        referenceElement.SetAttribute("Include", assemblyName);

        XmlElement hintPath = doc.CreateElement("HintPath");
        hintPath.InnerText = Path.Combine(binPath, _paths.DotNetVersionIdentifier, assemblyName + ".dll");
        referenceElement.AppendChild(hintPath);
        itemGroup.AppendChild(referenceElement);
    }

    private void AppendSourceGeneratorReference(XmlDocument doc, XmlElement itemGroup)
    {
        XmlElement sourceGeneratorReference = doc.CreateElement("Analyzer");
        sourceGeneratorReference.SetAttribute("Include", _paths.GetSourceGeneratorsAssemblyPath().FullName);
        itemGroup.AppendChild(sourceGeneratorReference);
    }

    private void AppendGeneratedCode(XmlDocument doc, XmlElement itemGroup)
    {
        var glueProjectFileFile = _paths.GetGlueProjectFile();
        string relativePath = PathUtils.GetRelativePath(_projectDirectory.FullName, glueProjectFileFile.FullName);

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }



    public void AddLaunchSettings()
    {
        var propertiesDirectory = new DirectoryInfo(Path.Combine(_projectDirectory.FullName, "Properties"));
        var launchSettingsFile = new FileInfo(Path.Combine(propertiesDirectory.FullName, "launchSettings.json"));
        
        if (launchSettingsFile.Exists)
        {
            return;
        }

        if (!propertiesDirectory.Exists)
        {
            propertiesDirectory.Create();
        }


        WriteLaunchSettingsFile(launchSettingsFile);
    }

    private void WriteLaunchSettingsFile(FileInfo launchSettingsPath)
    {
        var root = new Root();

        var unrealEditorFile = _paths.UnrealEditorFile;

        if(unrealEditorFile == null || !unrealEditorFile.Exists)
        {
            throw new FileNotFoundException($"Engine editor file not found at: {unrealEditorFile?.FullName}");
        }

        var unrealProjectFile = _paths.UnrealProjectFile;

        if (unrealProjectFile == null || !unrealProjectFile.Exists)
        {
            throw new FileNotFoundException($"Unreal project file not found at: {unrealProjectFile?.FullName}");
        }

        string commandLineArgs = PathUtils.GetEscapedPath(unrealProjectFile.FullName);

        // Create a new profile if it doesn't exist
        root.Profiles ??= new Profiles();

        root.Profiles.ProfileName = new Profile
        {
            CommandName = "Executable",
            ExecutablePath = unrealEditorFile.FullName,
            CommandLineArgs = commandLineArgs,
        };

        string newJsonString = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
        using var writer = File.CreateText(launchSettingsPath.FullName);
        writer.Write(newJsonString);
        writer.Close();
    }

    #region Launch Profiles

    #endregion

    public class Root
    {
        [JsonProperty("profiles")]
        public Profiles Profiles { get; set; } = new Profiles();
    }
    public class Profiles
    {
        [JsonProperty("UnrealSharp")]
        public Profile ProfileName { get; set; } = new Profile();
    }

    public class Profile
    {
        [JsonProperty("commandName")]
        public string CommandName { get; set; } = string.Empty;

        [JsonProperty("executablePath")]
        public string ExecutablePath { get; set; } = string.Empty;

        [JsonProperty("commandLineArgs")]
        public string CommandLineArgs { get; set; } = string.Empty;
    }

}
