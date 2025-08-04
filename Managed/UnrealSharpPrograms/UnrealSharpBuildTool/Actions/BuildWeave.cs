namespace UnrealSharpBuildTool.Actions;

public class BuildWeave(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {
        var buildSolution = new BuildUserSolution(_context);
        var weaveProject = new WeaveProject(_context);
        return buildSolution.RunAction() && weaveProject.RunAction() && AddLaunchSettings();
    } 

    bool AddLaunchSettings()
    {
        var allProjectFiles = _context.Paths.GetWeavableProjectFiles();
        var filteredProjectFiles = allProjectFiles.Where(p => p.DirectoryName != null && !p.DirectoryName.Equals(BuildToolConstants.GlueProjectName));

        foreach (FileInfo projectFile in filteredProjectFiles)
        {                                    
            if(projectFile.Directory == null)
            {
                _context.Logger.Warning($"Project file {projectFile.FullName} does not have a valid directory. Skipping launch settings addition.");
                continue;
            }

            var projectFileManager = new BuildToolProjectFileManager(projectFile.Directory, projectFile, _context.Options, _context.Paths, _context.Logger);
            projectFileManager.AddLaunchSettings();            
        }
        return true;
    }
    
}