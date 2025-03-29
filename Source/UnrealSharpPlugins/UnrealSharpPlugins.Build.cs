using UnrealBuildTool;

public class UnrealSharpPlugins : ModuleRules
{
    public UnrealSharpPlugins(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core", 
                "PluginBrowser",
            }
        );

        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "Slate",
                "SlateCore", 
                "GameProjectGeneration",
                "Projects"
            }
        );
    }
}