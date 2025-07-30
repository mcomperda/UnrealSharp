﻿using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution : BuildToolAction
{
    private readonly BuildConfig _buildConfig;
    private readonly string _folder;
    private readonly Collection<string>? _extraArguments;
    
    public BuildSolution(string folder, Collection<string>? extraArguments = null, BuildConfig buildConfig = BuildConfig.Debug)
    {
        _folder = PathUtils.GetNormalizedPath(folder);
        _buildConfig = buildConfig;
        _extraArguments = extraArguments;
    }
    
    public override bool RunAction()
    {
        if (!Directory.Exists(_folder))
        {
            throw new Exception($"Couldn't find the solution file at \"{_folder}\"");
        }
        
        BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
        if (_buildConfig == BuildConfig.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
        }
        else
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        }
        
        buildSolutionProcess.StartInfo.AddPath(_folder);
        
        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(Program.GetBuildConfiguration(_buildConfig));
        
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