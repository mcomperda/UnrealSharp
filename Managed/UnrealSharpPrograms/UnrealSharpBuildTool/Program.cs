using CommandLine;
using UnrealSharp.Tools;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharpBuildTool;


public static class Program
{
    private static readonly ToolLogger _logger = new("build_tool");

    public static int Main(string[] args)
    {
        try
        {

#if DEBUG_TOOL
            Console.WriteLine("Waiting for debugger to attach...");
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }

            Console.WriteLine("Debugger attached");
#endif
            _logger.Info($"Build tool launched with {args.Length} args");

            foreach (string arg in args)
            {
                _logger.Info($"Argument: {arg}");
            }

            Console.WriteLine(">>> UnrealSharpBuildTool");
            var parser = new Parser(with => with.HelpWriter = null);
            var result = parser.ParseArguments<BuildToolOptions>(args);

            if (result.Tag == ParserResultType.NotParsed)
            {
                BuildToolOptions.PrintHelp(result);

                string errors = string.Empty;
                foreach (Error error in result.Errors)
                {
                    if (error is TokenError tokenError)
                    {
                        errors += $"{tokenError.Tag}: {tokenError.Token} \n";
                    }
                    else if(error is MissingRequiredOptionError missingError)
                    {
                        
                        errors += $"Missing required arg: {missingError.NameInfo.LongName} \n";
                    }
                }

                if(errors.Length == 0)
                {
                    return 1;
                }

                throw new Exception($"Invalid arguments. Errors: {errors}");
            }

            result.Value.Migrate();
            var buildToolOptions = result.Value;
            var buildToolContext = new BuildToolContext(buildToolOptions, _logger);
            if (!BuildToolAction.InitializeAction(buildToolContext))
            {
                throw new Exception("Failed to initialize action.");
            }

            _logger.Info($"UnrealSharpBuildTool executed {buildToolOptions.Action} action successfully.");
        }
        catch (Exception exception)
        {
            _logger.Exception(exception, "A fatal exception occurred");            
            return 1;
        }
        finally
        {
            _logger.Shutdown();
        }

        return 0;
    }






}
