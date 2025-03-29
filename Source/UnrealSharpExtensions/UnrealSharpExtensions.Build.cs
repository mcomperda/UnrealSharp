using UnrealBuildTool;

public class UnrealSharpExtensions : ModuleRules
{
    public UnrealSharpExtensions(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core", 
            }
        );

        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "UnrealSharpCore",
                "GameplayTags",
                "DeveloperSettings",
                "Kismet"
            }
        );
    }
}