using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.Xml.Linq;

namespace UnrealSharpBuildTool;

public enum BuildAction : int
{
    Build,
    Clean,
    GenerateProject,
    Rebuild,
    Weave,
    PackageProject,
    GenerateSolution,
    BuildWeave,
}

public enum BuildConfig : int
{
    Debug,
    Release,
    Publish,
}

public class BuildToolOptions
{
    [Option("Action", Required = true, HelpText = "The action the build tool should process. Possible values: Build, Clean, GenerateProject, Rebuild, Weave, PackageProject, GenerateSolution, BuildWeave.")]
    public BuildAction Action { get; set; }
    
    [Option("DotNetPath", Required = false, HelpText = "The path to the dotnet.exe")]
    public string DotNetPath { get; set; } = string.Empty;
    
    [Option("ProjectDirectory", Required = true, HelpText = "The directory where the .uproject file resides.")]
    public string ProjectDirectory { get; set; } = string.Empty;

    [Option("ArchiveDirectory", Required = false, HelpText = "The directory where your packaged game resides.")]
    public string ArchiveDirectory { get; set; } = string.Empty;

    [Option("PluginDirectory", Required = false, HelpText = "The UnrealSharp plugin directory.")]
    public string PluginDirectory { get; set; } = string.Empty;
    
    [Option("EngineDirectory", Required = false, HelpText = "The Unreal Engine directory.")]
    public string EngineDirectory { get; set; } = string.Empty;
    
    [Option("ProjectName", Required = true, HelpText = "The name of the Unreal Engine project.")]
    public string ProjectName { get; set; } = string.Empty;
    
    [Option("AdditionalArgs", Required = false, HelpText = "Additional key-value arguments for the build tool.")]
    public IEnumerable<string> AdditionalArgs { get; set; } = new List<string>();

    public Version DotNetVersion { get; private set; } = Environment.Version;

   
    public string TryGetArgument(string argument)
    {
        foreach (var arg in AdditionalArgs)
        {
            if (!arg.StartsWith(argument))
            {
                continue;
            }
            
            return arg.Substring(argument.Length + 1);
        }
        
        return string.Empty;
    }    

    public bool HasArgument(string argument)
    {
        foreach (var arg in AdditionalArgs)
        {
            if (arg.StartsWith(argument))
            {
                return true;
            }
        }
        return false;
    }

    public static void PrintHelp(ParserResult<BuildToolOptions> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        Console.Error.WriteLine("Options:");

        var helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }

    public void Migrate()
    {
        if(ArchiveDirectory == string.Empty)
        {
            ArchiveDirectory = ArchiveDirectory = TryGetArgument("ArchiveDirectory");
        }
    }

    public string GetBuildConfigName()
    {
        string buildConfig = TryGetArgument("BuildConfig");
        if (string.IsNullOrEmpty(buildConfig))
        {
            buildConfig = "Debug";
        }
        return buildConfig;
    }

    public static string GetBuildConfigName(BuildConfig buildConfig)
    {
        return buildConfig switch
        {
            BuildConfig.Debug => "Debug",
            BuildConfig.Release => "Release",
            BuildConfig.Publish => "Release",
            _ => "Release"
        };
    }


    public BuildConfig ParseBuildConfig()
    {
        string buildConfig = GetBuildConfigName();
        if (!Enum.TryParse(buildConfig, out BuildConfig config))
        {
            throw new ArgumentException($"Invalid build configuration: {buildConfig}. Expected values are: Debug, Release, Publish.");
        }
        return config;
    }
}