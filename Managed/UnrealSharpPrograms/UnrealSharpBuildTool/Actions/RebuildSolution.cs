using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class RebuildSolution(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {
        var clean = new CleanSolution(_context);
        
        if (!clean.RunAction())
        {
            return false;
        }

        var buildSolution = new BuildUserSolution(_context);
        
        if (!buildSolution.RunAction())
        {
            return false;
        }

        return true;
    }
}