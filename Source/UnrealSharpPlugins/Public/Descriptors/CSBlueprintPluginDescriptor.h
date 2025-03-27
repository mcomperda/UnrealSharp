// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPluginDescriptor.h"
#include "CSBlueprintPluginDescriptor.generated.h"

UCLASS(NotBlueprintable)
class UNREALSHARPPLUGINS_API UCSBlueprintPluginDescriptor : public UCSPluginDescriptor
{
	GENERATED_BODY()
public:
	// UCSPluginDescriptor interface
	virtual FString GetPluginName() const override;
	virtual FString GetPluginDescription() const override;
	virtual TArray<FString> GetTemplatePaths() const override;
	virtual bool CanContainContent() const override;
	virtual void OnPluginCreated() override;
	// End of UCSPluginDescriptor interface
protected:
	UFUNCTION(BlueprintImplementableEvent)
	FString K2_GetPluginName() const;

	UFUNCTION(BlueprintImplementableEvent)
	FString K2_GetPluginDescription() const;

	UFUNCTION(BlueprintImplementableEvent)
	TArray<FString> K2_GetTemplatePaths() const;

	UFUNCTION(BlueprintImplementableEvent)
	bool K2_CanContainContent() const;

	UFUNCTION(BlueprintImplementableEvent)
	void K2_OnPluginCreated();
};
