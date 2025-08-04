using System.Collections.ObjectModel;
using UnrealSharp.Tools;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution : BuildToolAction
{
    private readonly BuildConfig _buildConfig;
    private readonly DirectoryInfo _solutionDirectory;
    private readonly Collection<string>? _extraArguments;

    public DirectoryInfo Folder => _solutionDirectory;

    public BuildSolution(BuildToolContext ctx, DirectoryInfo solutionDirectory, Collection<string>? extraArguments = null, BuildConfig buildConfig = BuildConfig.Debug)
        : base(ctx)
    {
        _solutionDirectory = solutionDirectory;
        _buildConfig = buildConfig;
        _extraArguments = extraArguments;
    }
    
    protected override bool DoRunAction()
    {
        _context.Logger.Info($"[BuildSolution] Folder: {_solutionDirectory.FullName}");
        _context.Logger.Info($"[BuildSolution] Config: {_buildConfig}");
        _context.Logger.Info($"[BuildSolution] Extra Args: {_extraArguments}");

        if(!_solutionDirectory.Exists)
        {
            _context.Logger.Error($"Solution directory does not exist: {_solutionDirectory.FullName}");
            return false;
        }

        var solutionFiles = _solutionDirectory.GetFiles("*.sln");
        
        if(solutionFiles.Length == 0)
        {
            _context.Logger.Error($"No solution files found in directory: {_solutionDirectory.FullName}");
            return false;
        }
        if (solutionFiles.Length > 1)
        {
            _context.Logger.Error($"More than one solution file found in directory: {_solutionDirectory.FullName}");
            return false;
        }

        var buildSolutionProcess = new BuildToolProcess(_context);
        
        if (_buildConfig == BuildConfig.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
        }
        else
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        }
        
        buildSolutionProcess.StartInfo.AddPath(_solutionDirectory.FullName);
        buildSolutionProcess.StartInfo.WorkingDirectory = _solutionDirectory.FullName;

        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(_context.Options.GetBuildConfigName());

        //buildSolutionProcess.StartInfo.ArgumentList.Add("--verbosity");
        //buildSolutionProcess.StartInfo.ArgumentList.Add("detailed");

        buildSolutionProcess.StartInfo.ArgumentList.Add("--framework");
        buildSolutionProcess.StartInfo.ArgumentList.Add(_context.Paths.DotNetVersionIdentifier);

        if (_extraArguments != null)
        {
            foreach (var argument in _extraArguments)
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add(argument);
            }
        }

        return buildSolutionProcess.StartBuildToolProcess();
    }
}