using UnrealSharp.Tools;

namespace UnrealSharpWeaver;

public static class Program
{    
    private static readonly ToolLogger _logger = new("weaver");   

    public static int Main(string[] args)
    {
        try 
        {            
            WeaveAssemblies(WeaverOptions.ParseArguments(args));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static bool WeaveAssemblies(WeaverOptions options)
    {
        try
        {            
            _logger.Info($"Weaver launched with {options.AssemblyPaths} assemblies to process.");            
            var context = new WeaverContext(options, _logger);
            var weaver = new Weaver(context);
            weaver.Run();            
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred during weaving: {ex.Message}");
            return false;
        }
        finally
        {
            
        }
    }

    
}
