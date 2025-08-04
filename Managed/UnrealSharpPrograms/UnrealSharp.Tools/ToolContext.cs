namespace UnrealSharp.Tools;

public abstract class ToolContext(ToolLogger logger)
{
    public Version DotNetVersion { get; private set; } = Environment.Version;
    public ToolLogger Logger { get; private set; } = logger;
}
