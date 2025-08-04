using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildUserSolution(BuildToolContext ctx, Collection<string>? extraArguments = null, BuildConfig buildConfig = BuildConfig.Debug) 
    : BuildSolution(ctx, ctx.Paths.ScriptDirectory, extraArguments, buildConfig);
