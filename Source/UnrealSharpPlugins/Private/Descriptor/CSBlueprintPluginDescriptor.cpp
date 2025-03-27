#include "Descriptors/CSBlueprintPluginDescriptor.h"

FString UCSBlueprintPluginDescriptor::GetPluginName() const
{
	return K2_GetPluginName();
}

FString UCSBlueprintPluginDescriptor::GetPluginDescription() const
{
	return K2_GetPluginDescription();
}

TArray<FString> UCSBlueprintPluginDescriptor::GetTemplatePaths() const
{
	return K2_GetTemplatePaths();
}

bool UCSBlueprintPluginDescriptor::CanContainContent() const
{
	return K2_CanContainContent();
}

void UCSBlueprintPluginDescriptor::OnPluginCreated()
{
	K2_OnPluginCreated();
}
