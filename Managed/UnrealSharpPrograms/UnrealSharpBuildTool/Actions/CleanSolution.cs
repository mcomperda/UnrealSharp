namespace UnrealSharpBuildTool.Actions;

public class CleanSolution(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {        
        var unrealSharpBinaries = _context.Paths.GetManagedBinariesPublishDirectory();

        _context.Logger.Info($"Deleting folder at: {unrealSharpBinaries.FullName}");

        if (unrealSharpBinaries.Exists)
        {
            unrealSharpBinaries.Delete(true);            
        }
        
        return true;
    }
}