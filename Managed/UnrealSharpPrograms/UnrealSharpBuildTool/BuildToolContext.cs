using UnrealSharp.Tools;

namespace UnrealSharpBuildTool;

public class BuildToolContext(BuildToolOptions options, ToolLogger logger) : ToolContext(logger)
{
    public BuildToolOptions Options { get; private set; } = options;

    public BuildToolPaths Paths { get; private set; } = new BuildToolPaths(options, logger);
}
