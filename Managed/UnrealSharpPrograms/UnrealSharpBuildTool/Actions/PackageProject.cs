using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PackageProject(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {
        var archiveDirectory = _context.Paths.GetArchiveDirectory();        
        
        // Where to publish the binaries to
        var publishPath = _context.Paths.GetManagedBinariesPublishDirectory();
        
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
			"-p:DefineAdditionalConstants=PACKAGE",
            $"-p:PublishDir=\"{publishPath}\""
        ];

        var buildBindings = new BuildSolution(_context, _context.Paths.BindingsSolutionDirectory, extraArguments, BuildConfig.Publish);
        if(!buildBindings.RunAction())
        {
            _context.Logger.Warning($"Failed to build and publish bindings solution at: {buildBindings.Folder}");
            return false;
        }
        
        var buildUserSolution = new BuildUserSolution(_context, null, BuildConfig.Publish);        
        if (!buildUserSolution.RunAction())
        {
            _context.Logger.Warning($"Failed to build and publish user solution at: {buildUserSolution.Folder}");
            return false;
        }

        var weaveProject = new WeaveProject(_context);
        if(!weaveProject.RunAction())
        {
            _context.Logger.Warning($"Failed to weave project!");
            return false;
        }
        weaveProject.RunAction();
        
        return true;
    }
}
