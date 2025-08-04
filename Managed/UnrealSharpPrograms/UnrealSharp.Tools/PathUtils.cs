using System.Diagnostics;


namespace UnrealSharp.Tools;

public static class PathUtils
{
    public static string GetRelativePath(string basePath, string targetPath)
    {
        var baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? basePath
            : basePath + Path.DirectorySeparatorChar);
        var targetUri = new Uri(targetPath);
        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        return OperatingSystem.IsWindows() ? Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\') 
            : Uri.UnescapeDataString(relativeUri.ToString());
    }

    public static string StripQuotes(string value)
    {
        if (value.StartsWith('\"') && value.EndsWith('\"'))
        {
            return value[1..^1];
        }

        return value;
    }


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
