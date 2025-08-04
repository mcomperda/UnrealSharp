using System.Xml.Linq;
using UnrealSharp.Tools;

namespace UnrealSharpBuildTool;

public enum BuildToolOutputMode
{
    Development,
    Packaged
}

public class BuildToolPaths
{
    private readonly BuildToolOutputMode _outputMode = BuildToolOutputMode.Development;
    private readonly BuildToolOptions _options;
    private readonly ToolLogger _logger;
    private readonly PathHelper _pathHelper;

    /// <summary>
    /// The directory where the Unreal project resides (.uproject file).
    /// </summary>
    private readonly DirectoryInfo _projectDirectory;

    /// <summary>
    /// The directory where the UnrealSharp plugin should be
    /// </summary>
    private readonly DirectoryInfo _pluginDirectory;

    public DirectoryInfo PluginDirectory => _pluginDirectory;


    /// <summary>
    /// The directory where the user's c# project resides (a subfolder of the Unreal project directory).
    /// </summary>
    public DirectoryInfo ScriptDirectory => _scriptDirectory;
    private readonly DirectoryInfo _scriptDirectory;

    public string ScriptSolutionName => _scriptSolutionName;
    private readonly string _scriptSolutionName;

    private readonly string _scriptSolutionFilename;

    private readonly DirectoryInfo _engineDirectory;
    public DirectoryInfo EngineDirectory => _engineDirectory;

    public FileInfo? UnrealEditorFile => _engineEditorFile;
    private readonly FileInfo? _engineEditorFile;

    public FileInfo? UnrealProjectFile => _unrealProjectFile;
    private readonly FileInfo? _unrealProjectFile;

    private readonly string _dotNetVersionIdentifier;
    public string DotNetVersionIdentifier => _dotNetVersionIdentifier;

    private readonly string _buildConfigName;
    public string BuildConfigName => _buildConfigName;


    private readonly DirectoryInfo _bindingsSolutionDirectory;
    public DirectoryInfo BindingsSolutionDirectory => _bindingsSolutionDirectory;


    public BuildToolPaths(BuildToolOptions options, ToolLogger logger)
    {
        _logger = logger;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dotNetVersionIdentifier = $"net{options.DotNetVersion.Major}.{options.DotNetVersion.Minor}";
        _buildConfigName = _options.GetBuildConfigName();
        _pathHelper = new PathHelper(logger);

        _projectDirectory = _pathHelper.GetRequiredDirectory(_options.ProjectDirectory, nameof(_options.ProjectDirectory));
        _unrealProjectFile = _pathHelper.TryGetFileInfo(_projectDirectory, $"{_options.ProjectName}.uproject", "Unreal Editor", true);
        _pluginDirectory = _pathHelper.GetOrCreateSubDirectory(_projectDirectory, BuildToolConstants.UnrealSharpPluginsFolder, "Plugins Directory");
        _scriptDirectory = _pathHelper.GetOrCreateSubDirectory(_projectDirectory, BuildToolConstants.UserProjectFolderName, "Script Directory");
        _scriptSolutionName = $"Managed{_options.ProjectName}";
        _scriptSolutionFilename = $"{_scriptSolutionName}.sln";
        _engineDirectory = _pathHelper.GetRequiredDirectory(_options.EngineDirectory, nameof(_options.EngineDirectory));
        _engineEditorFile = _pathHelper.TryGetFileInfo(_engineDirectory, BuildToolConstants.UnrealEditorFilePath, "Unreal Editor", true);

        _bindingsSolutionDirectory = _pathHelper.GetOrCreateSubDirectory(_projectDirectory, BuildToolConstants.UnrealSharpBindingsSolutionDirectory, "Bindings Solution Directory");
       
        if (_options.Action == BuildAction.PackageProject)
        {
            _outputMode = BuildToolOutputMode.Packaged;
        }
    }


    public DirectoryInfo GetArchiveDirectory()
    {
        return _pathHelper.GetRequiredDirectory(_options.ArchiveDirectory, nameof(_options.ArchiveDirectory));
    }


    /// <summary>
    /// The directory where the managed binaries of the UnrealSharp plugin are stored.
    /// </summary>
    /// <returns></returns>
    public DirectoryInfo GetDevelopmentAssembliesDirectory()
    {
        return _pathHelper.GetOrCreateSubDirectory(_pluginDirectory, BuildToolConstants.ManagedBinariesFolderPath, "Bindings Development Assemblies");
    }



    /// <summary>
    /// The fully qualified path to publish binaries to.
    /// In development, it is the 
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public DirectoryInfo GetManagedBinariesPublishDirectory()
    {
        var parent = _outputMode == BuildToolOutputMode.Packaged
            ? GetArchiveDirectory()
            : _projectDirectory;

        return _pathHelper.GetOrCreateSubDirectory(parent, BuildToolConstants.ManagedBinariesFolderPath, "Bindings Publish Directory");
    }

    public FileInfo GetSourceGeneratorsAssemblyPath()
    {
        var pluginBinariesDirectory = GetDevelopmentAssembliesDirectory();
        return _pathHelper.TryGetFileInfo(pluginBinariesDirectory, BuildToolConstants.SourceGeneratorsAssemblyFileName, "Source Generators DLL", true)
               ?? throw new FileNotFoundException($"Source Generators DLL not found in {_pluginDirectory.FullName}");
    }

    public FileInfo GetScriptSolutionFile()
    {
        return _pathHelper.TryGetFileInfo(_scriptDirectory, _scriptSolutionFilename, "Project Solution", true)
               ?? throw new FileNotFoundException($"Solution file for project {_options.ProjectName} not found in {_scriptDirectory.FullName}");
    }

    public FileInfo GetNewScriptProjectFile(string name)
    {
        var projectDirectory = GetNewScriptProjectDirectory(name);
        return _pathHelper.GetFileInfo(projectDirectory, $"{name}.csproj", "New Project File");
    }

    public FileInfo GetScriptProjectAssemblyFile(FileInfo projectFile)
    {

        // Get the parent folder for the project
        var projectDirectory = projectFile.Directory;

        if (projectDirectory == null || !projectDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Project directory for file: {projectFile.FullName} does not exist.");
        }

        var projectName = Path.GetFileNameWithoutExtension(projectFile.Name);
        var projectAssemblyFileName = $"{projectName}.dll";
        var projectAssemblyPath = Path.Combine(projectDirectory.FullName, "bin", _buildConfigName, _dotNetVersionIdentifier, projectAssemblyFileName);
        return new FileInfo(projectAssemblyPath);
    }

    public DirectoryInfo GetGlueProjectDirectory()
    {
        return _pathHelper.GetOrCreateSubDirectory(_scriptDirectory, BuildToolConstants.GlueProjectName, "Glue Project Directory");
    }

    public FileInfo GetGlueProjectFile()
    {
        return _pathHelper.GetFileInfo(GetGlueProjectDirectory(), BuildToolConstants.GlueProjectFilename, "Glue Project File");
    }

    public DirectoryInfo GetNewScriptProjectDirectory(string name)
    {
        return _pathHelper.GetOrCreateSubDirectory(_scriptDirectory, name, "New Script Project Directory");
    }

    public IReadOnlyList<FileInfo> GetWeavableProjectFiles()
    {
        var folderInfo = _scriptDirectory;
        var csProjectFiles = folderInfo.GetFiles("*.csproj", SearchOption.AllDirectories);
        var fsProjectFiles = folderInfo.GetFiles("*.fsproj", SearchOption.AllDirectories);

        List<FileInfo> allProjectFiles = new(csProjectFiles.Length + fsProjectFiles.Length);
        allProjectFiles.AddRange(csProjectFiles.Where(IsWeavableProject));
        allProjectFiles.AddRange(fsProjectFiles.Where(IsWeavableProject));
        return allProjectFiles;
    }

    private static bool IsWeavableProject(FileInfo projectFile)
    {
        // We need to be able to filter out certain non-production projects.
        // The main target of this is source generators and analyzers which users
        // may want to leverage as part of their solution and can't be weaved because
        // they have to use netstandard2.0.
        XDocument doc = XDocument.Load(projectFile.FullName);
        return !doc.Descendants()
            .Where(element => element.Name.LocalName == "PropertyGroup")
            .SelectMany(element => element.Elements())
            .Any(element => element.Name.LocalName == "ExcludeFromWeaver" &&
                            element.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}
