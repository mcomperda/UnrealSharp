using System.Diagnostics;
using System.Text;

namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{
    public BuildToolProcess(string? fileName = null)
    {
        if (fileName == null)
        {
            if (string.IsNullOrEmpty(Program.BuildToolOptions.DotNetPath))
            {
                fileName = "dotnet";
            }
            else
            {
                fileName = Program.BuildToolOptions.DotNetPath;
            }
        }                

        StartInfo.FileName = fileName;
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
        Console.WriteLine();
        Console.WriteLine(commandLine);        
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

            ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Console.Error.WriteLine(args.Data);
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

            string error = StandardError.ReadToEnd();
            
            WaitForExit();

            if (ExitCode != 0)
            {
                throw new Exception($"Error in executing build command. FileName: {StartInfo.FileName}, WorkingDirectory: {StartInfo.WorkingDirectory} Args: {string.Join(",", StartInfo.ArgumentList)}: {Environment.NewLine + error + Environment.NewLine + output}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }

        return true;
    }
}