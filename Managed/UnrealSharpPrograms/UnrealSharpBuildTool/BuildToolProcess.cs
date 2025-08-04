using System.Diagnostics;
using System.Text;
using UnrealSharp.Tools;
namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{
    private readonly BuildToolContext _context;

    public BuildToolProcess(BuildToolContext ctx, string? processFileName = null)
    {
        _context = ctx;

        if (processFileName == null)
        {
            if (string.IsNullOrEmpty(_context.Options.DotNetPath))
            {
                processFileName = "dotnet";
            }
            else
            {
                processFileName = _context.Options.DotNetPath;
            }
        }

        StartInfo.FileName = PathUtils.GetEscapedPath(processFileName);
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        
    }

    private void WriteOutProcess()
    {
        string command = StartInfo.FileName;
        string arguments = string.Join(" ", StartInfo.ArgumentList);
        var commandLine = $"Command: {command} {arguments}";
        _context.Logger.Info(commandLine);        
    }

    public bool StartBuildToolProcess()
    {
        try
        {
            if (!Start())
            {
                WaitForExit();
                throw new Exception("Failed to start process");
            }

            WriteOutProcess();

            StringBuilder output = new();
            StringBuilder errors = new();

            ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    errors.AppendLine(args.Data);
                }
            };

            OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };

            // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
            BeginOutputReadLine();            
            WaitForExit();

            _context.Logger.Info($"Build process completed with exit code: {ExitCode}");

            if (ExitCode != 0)
            {
                _context.Logger.Info($"Build process output: {output}");
                _context.Logger.Error($"Build process error: {errors}");
                throw new Exception($"Error in executing build command. FileName: {StartInfo.FileName}, WorkingDirectory: {StartInfo.WorkingDirectory} Args: {string.Join(",", StartInfo.ArgumentList)}: {Environment.NewLine + errors + Environment.NewLine + output}");
            }
        }
        catch (Exception ex)
        {
            _context.Logger.Exception(ex, "An error occurred while starting the build tool process");
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }

        return true;
    }
}