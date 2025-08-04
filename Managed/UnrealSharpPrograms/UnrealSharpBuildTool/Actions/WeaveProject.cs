using UnrealSharpWeaver;

namespace UnrealSharpBuildTool.Actions;

public class WeaveProject(BuildToolContext ctx) : BuildToolAction(ctx)
{
    protected override bool DoRunAction()
    {
        var files = _context.Paths.GetWeavableProjectFiles();
        if (files.Count == 0)
        {
            Console.WriteLine("No project files found. Skipping weaving...");
            return true;
        }

        var paths = new HashSet<string>();

        foreach (var projectFile in files)
        {

            FileInfo assemblyFile = _context.Paths.GetScriptProjectAssemblyFile(projectFile);

            if (!assemblyFile.Exists)
            {
                Console.WriteLine($"Assembly {assemblyFile.FullName} does not exist. Skipping weaving for this project.");
                continue;
            }

            paths.Add(assemblyFile.FullName);
        }

        if (paths.Count == 0)
        {
            _context.Logger.Warning("No valid projects found to weave. Skipping weaving...");
            return true;
        }

        var weaveOptions = new WeaverOptions
        {
            AssemblyPaths = paths,
            OutputDirectory = _context.Paths.GetManagedBinariesPublishDirectory().FullName
        };
        
        var context = new WeaverContext(weaveOptions, _context.Logger);
        var weaver = new Weaver(context);
        try
        {
            weaver.Run();
            return true;
        }
        catch (Exception e)
        {
            _context.Logger.Error($"Weaving failed: {e.Message}");
            return false;
        }
    }

}