#include "UnrealSharpPlugins.h"
#include "GameProjectGenerationModule.h"
#include "ProjectDescriptor.h"
#include "Interfaces/IPluginManager.h"
#include "Interfaces/IProjectManager.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpPluginsModule"

void FUnrealSharpPluginsModule::StartupModule()
{
	GatherManagedPlugins();
}

void FUnrealSharpPluginsModule::ShutdownModule()
{
    
}

void FUnrealSharpPluginsModule::InvokePluginWizard()
{
	FGlobalTabmanager::Get()->TryInvokeTab(UnrealSharpPlugins::PluginTabName);
}

void FUnrealSharpPluginsModule::GatherManagedPlugins()
{
	ManagedPlugins.Reset();
	
	IFileManager& FileManager = IFileManager::Get();
	IPluginManager& PluginManager = IPluginManager::Get();
		
	for (TSharedRef<IPlugin>& Plugin : PluginManager.GetDiscoveredPlugins())
	{
		FString ScriptPath = FPaths::ConvertRelativePathToFull(Plugin->GetBaseDir() / TEXT("Script"));
		
		if (!FileManager.DirectoryExists(*ScriptPath))
		{
			// This plugin doesn't have a script directory, so it's not a managed plugin
			continue;
		}

		ManagedPlugins.Add(FCSManagedPluginInfo(ScriptPath, Plugin));
	}
}

bool FUnrealSharpPluginsModule::IsContentOnlyProject()
{
	const FProjectDescriptor* CurrentProject = IProjectManager::Get().GetCurrentProject();
	return CurrentProject == nullptr || CurrentProject->Modules.Num() == 0 || !FGameProjectGenerationModule::Get().ProjectHasCodeFiles();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpPluginsModule, UnrealSharpPlugins)