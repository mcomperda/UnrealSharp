namespace UnrealSharpBuildTool.Actions;

public class GenerateSolution(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {
        BuildToolProcess generateSln = new BuildToolProcess(_context);
        
        // Create a solution.
        generateSln.StartInfo.ArgumentList.Add("new");
        generateSln.StartInfo.ArgumentList.Add("sln");
        
        // Assign project name to the solution.
        generateSln.StartInfo.ArgumentList.Add("-n");
        generateSln.StartInfo.ArgumentList.Add(ctx.Paths.ScriptSolutionName);
        generateSln.StartInfo.WorkingDirectory = ctx.Paths.ScriptDirectory.FullName;
        
        // Force the creation of the solution.
        generateSln.StartInfo.ArgumentList.Add("--force");
        generateSln.StartBuildToolProcess();
        
        var existingProjects = _context.Paths.ScriptDirectory.GetFiles("*.csproj", SearchOption.AllDirectories);
        var existingProjectsList = new HashSet<string>();
        
        foreach (var projectFile in existingProjects)
        {            
            string relativePath = Path.GetRelativePath(_context.Paths.ScriptDirectory.FullName, projectFile.FullName);
            existingProjectsList.Add(relativePath);
        }
        
        GenerateProject.AddProjectsToSln(_context, existingProjectsList);
        return true;
    }
}