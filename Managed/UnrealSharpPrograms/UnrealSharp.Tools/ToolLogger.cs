using Serilog;
using Serilog.Core;

namespace UnrealSharp.Tools;
public class ToolLogger : IDisposable
{
    protected readonly Logger _serilog;
    protected readonly ILogger _logger;
    private bool _disposedValue;

    public ToolLogger(string name)
    {
        var fileName = $"{name}.log";

        if(File.Exists(fileName))
        {
            File.Delete(fileName); // Ensure we start with a fresh log file
        }           

        _serilog = new LoggerConfiguration()
           .WriteTo.File(fileName)
           .WriteTo.Console()
           .MinimumLevel.Debug()
           .CreateLogger();

        
        _logger = GetLogger();
        
        _logger.Information("Logger initialized");
    }

    protected virtual ILogger GetLogger()
    {
        return _serilog.ForContext<ToolLogger>();
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Warning(string message)
    {
        _logger.Warning(message);
    }

    public void Exception(Exception ex, string message)
    {
        _logger.Error(ex, message);
    }
    public void Error(string message)
    {
        _logger.Error(message);
    }

    public void Shutdown()
    {
        _logger?.Information("Logger shutting down");
        _serilog?.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Shutdown();
            }
            
            _disposedValue = true;
        }
    }


    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class ToolLogger<T>(string name) : ToolLogger(name)
{
    protected override ILogger GetLogger()
    {
        return _serilog.ForContext<T>();
    }

}
