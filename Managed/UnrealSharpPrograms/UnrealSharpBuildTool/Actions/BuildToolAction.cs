using Serilog.Core;

namespace UnrealSharpBuildTool.Actions;

public abstract class BuildToolAction(BuildToolContext ctx)
{
    protected readonly BuildToolContext _context = ctx;

    public static bool InitializeAction(BuildToolContext ctx)
    {        
        BuildToolAction buildToolAction = ctx.Options.Action switch
        {
            BuildAction.Build => new BuildUserSolution(ctx),
            BuildAction.Clean => new CleanSolution(ctx),
            BuildAction.GenerateProject => new GenerateProject(ctx),
            BuildAction.Rebuild => new RebuildSolution(ctx),
            BuildAction.Weave => new WeaveProject(ctx),
            BuildAction.PackageProject => new PackageProject(ctx),
            BuildAction.GenerateSolution => new GenerateSolution(ctx),
            BuildAction.BuildWeave => new BuildWeave(ctx),
            _ => throw new Exception($"Can't find build action with name \"{ctx.Options.Action}\"")
        };

        return buildToolAction.RunAction();
    }

    public bool RunAction()
    {
        _context.Logger.Info($"Running action: {GetType().Name}");
        try
        {
            return DoRunAction();
        }
        catch (Exception ex)
        {
            _context.Logger.Exception(ex, $"Failed to run build action: {GetType().Name}");
            return false;
        }

    }

    protected abstract bool DoRunAction();
}