namespace UnrealSharp.Tools;

public class PathHelper(ToolLogger logger)
{
    private readonly ToolLogger _logger = logger;

    public DirectoryInfo GetRequiredDirectory(string folderPath, string logName)
    {
        var di = TryGetDirectory(folderPath, logName, true);

        if (di == null)
        {
            throw new DirectoryNotFoundException($"Required directory {logName} not found at: {folderPath}");
        }

        return di;
    }

    public DirectoryInfo GetOrCreateSubDirectory(DirectoryInfo parent, string folderName, string logName)
    {
        if (!parent.Exists)
        {
            throw new DirectoryNotFoundException($"Required parent directory for {folderName} not found at: {parent.FullName}");
        }

        var di = new DirectoryInfo(Path.Combine(parent.FullName, folderName));

        if (!di.Exists)
        {
            _logger.Info($"Creating sub directory for '{logName}' at: {di.FullName}");
            di.Create();

        }
        else
        {
            _logger.Info($"Using existing subdirectory for '{logName}' at: {di.FullName}");
        }

        return di;
    }

    public DirectoryInfo? TryGetDirectory(string folderPath, string logName, bool mustExist)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            _logger.Warning($"No directory path supplied for {logName}");
            return null;
        }

        if (!Path.IsPathFullyQualified(folderPath))
        {
            _logger.Warning($"Expected fully qualified path for {logName}, but received full path: {folderPath}");
            return null;
        }

        var di = new DirectoryInfo(folderPath);

        if (mustExist && !di.Exists)
        {
            _logger.Warning($"Required directory {logName} not found at: {folderPath}");
            return null;
        }

        return di;
    }

    public FileInfo GetFileInfo(DirectoryInfo directory, string relativeFilePath, string logName)
    {
        var fi = TryGetFileInfo(directory, relativeFilePath, logName, false);
        return fi ?? throw new FileNotFoundException($"File {logName} not found at: {Path.Combine(directory.FullName, relativeFilePath)}");
    }

    public FileInfo? TryGetFileInfo(DirectoryInfo directory, string relativeFilePath, string logName, bool mustExist)
    {
        if (string.IsNullOrEmpty(directory.FullName))
        {
            _logger.Warning($"No directory path supplied for {logName}");
            return null;
        }

        if (!directory.Exists)
        {
            _logger.Warning($"Directory path supplied for {logName} does not exist: {directory.FullName}");
            return null;
        }

        if (Path.IsPathFullyQualified(relativeFilePath))
        {
            _logger.Warning($"Expected relative file path for {logName}, but received full path: {relativeFilePath}");
            return null;
        }

        var fileInfo = new FileInfo(Path.Combine(directory.FullName, relativeFilePath));

        if (mustExist && !fileInfo.Exists)
        {
            _logger.Warning($"Required file {logName} not found at: {fileInfo.FullName}");
            return null;
        }

        return fileInfo;

    }

}
