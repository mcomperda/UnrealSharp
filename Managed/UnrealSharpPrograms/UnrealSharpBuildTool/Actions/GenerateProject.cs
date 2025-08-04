namespace UnrealSharpBuildTool.Actions;

public class GenerateProject : BuildToolAction
{
    private readonly string _projectName;
    private readonly DirectoryInfo _projectDirectory;
    private readonly FileInfo _projectFile;
    private readonly BuildToolProjectFileManager _projectFileManager;

    public GenerateProject(BuildToolContext ctx) : base(ctx)
    {
        _projectName = _context.Options.TryGetArgument("NewProjectName");
        _projectDirectory = _context.Paths.GetNewScriptProjectDirectory(_projectName);
        _projectFile = _context.Paths.GetNewScriptProjectFile(_projectName);
        _projectFileManager = new BuildToolProjectFileManager(_projectDirectory, _projectFile, _context.Options, _context.Paths, _context.Logger);
    }

    protected override bool DoRunAction()
    {                
        if(Path.GetInvalidPathChars().Any(c => _projectName.Contains(c)))
        {
            throw new InvalidOperationException($"Project name {_projectName} contains invalid characters.");
        }
        
        if(_projectDirectory.GetFiles().Length > 0 || _projectDirectory.GetDirectories().Length > 0)
        {
            throw new InvalidOperationException($"Project directory {_projectDirectory.FullName} is not empty. Please choose a different project name or clean the directory.");
        }
                
        var generateProjectProcess = new BuildToolProcess(_context);
        
        // Create a class library.
        generateProjectProcess.StartInfo.ArgumentList.Add("new");
        generateProjectProcess.StartInfo.ArgumentList.Add("classlib");
        
        // Assign project name to the class library.
        generateProjectProcess.StartInfo.ArgumentList.Add("-n");
        generateProjectProcess.StartInfo.ArgumentList.Add(_projectName);
        
        // Set the target framework to the current version.
        generateProjectProcess.StartInfo.ArgumentList.Add("-f");
        generateProjectProcess.StartInfo.ArgumentList.Add(_context.Paths.DotNetVersionIdentifier);
        
        generateProjectProcess.StartInfo.WorkingDirectory = _projectDirectory.FullName;

        if (!generateProjectProcess.StartBuildToolProcess())
        {
            return false;
        }
                    
        // dotnet new class lib generates a file named Class1, remove it.
        string myClassFile = Path.Combine(_projectDirectory.FullName, "Class1.cs");
        if (File.Exists(myClassFile))
        {
            File.Delete(myClassFile);
        }
        
        var slnPath = _context.Paths.GetScriptSolutionFile();
        if (!slnPath.Exists)
        {
            var generateSolution = new GenerateSolution(_context);
            generateSolution.RunAction();
        }
        
        if (_context.Options.HasArgument("SkipUSharpProjSetup"))
        {
            return true;
        }

        _projectFileManager.AddLaunchSettings();
        _projectFileManager.WriteProjectFile();
        
        string relativePath = Path.GetRelativePath(_context.Paths.ScriptDirectory.FullName, _projectFile.FullName);
        AddProjectToSln(_context, relativePath);

        return true;
    }

    public static void AddProjectToSln(BuildToolContext context, string relativePath)
    {
        AddProjectsToSln(context, [relativePath]);
    }
    
    public static void AddProjectsToSln(BuildToolContext context, IReadOnlyCollection<string> relativePaths)
    {
        var addProjectToSln = new BuildToolProcess(context);
        addProjectToSln.StartInfo.ArgumentList.Add("sln");
        addProjectToSln.StartInfo.ArgumentList.Add("add");
        
        foreach (string relativePath in relativePaths)
        {
            addProjectToSln.StartInfo.ArgumentList.Add(relativePath);
        }

        addProjectToSln.StartInfo.WorkingDirectory = context.Paths.ScriptDirectory.FullName;
        addProjectToSln.StartBuildToolProcess();
    }

    
    
    
   
}

