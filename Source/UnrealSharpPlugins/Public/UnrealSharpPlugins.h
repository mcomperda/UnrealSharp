#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FCSPluginWizardDefinition;

namespace UnrealSharpPlugins
{
    inline static const FName PluginTabName = FName(TEXT("UnrealSharpPlugins"));
}

struct FCSManagedPluginInfo
{
    FCSManagedPluginInfo(const FString& InScriptAbsolutePath, TSharedPtr<IPlugin> InPlugin)
        : ScriptAbsolutePath(InScriptAbsolutePath)
        , Plugin(InPlugin)
    {
    }
    
    FString ScriptAbsolutePath;
    TSharedPtr<IPlugin> Plugin;
};

class FUnrealSharpPluginsModule : public IModuleInterface
{
public:

    // IModuleInterface interface
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End

    void InvokePluginWizard();
    void GatherManagedPlugins();
    
    static bool IsContentOnlyProject();

private:
    TSharedPtr<FCSPluginWizardDefinition> PluginWizard;
    TArray<FCSManagedPluginInfo> ManagedPlugins;
};
