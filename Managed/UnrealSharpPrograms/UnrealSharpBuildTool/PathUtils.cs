using System.Diagnostics;


namespace UnrealSharpBuildTool;

public static class PathUtils
{

    public static string GetNormalizedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Do nothing if the path is not fully qualified
        if (!Path.IsPathFullyQualified(path))
        {
            return path;
        }

        return Path.GetFullPath(path);

    }

    public static string GetEscapedPath(string path)
    {
        var normalizedPath = GetNormalizedPath(path);
        if (OperatingSystem.IsWindows())
        {
            return $"\"{normalizedPath}\"";
        }
        else // TODO: test and ensure this works properly on macOS and Linux
        {
            return normalizedPath;
        }


    }

    public static void AddPath(this ProcessStartInfo processStartInfo, string path)
    {
        processStartInfo.ArgumentList.Add(GetEscapedPath(path));
    }
}
