// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "CSPluginDescriptor.generated.h"

UCLASS()
class UNREALSHARPPLUGINS_API UCSPluginDescriptor : public UObject
{
	GENERATED_BODY()

public:
	// UCSPluginDescriptor interface
	virtual FString GetPluginName() const { return FString(); }
	virtual FString GetPluginDescription() const { return FString(); }
	virtual TArray<FString> GetTemplatePaths() const { return TArray<FString>(); }
	virtual bool CanContainContent() const { return false; }
	virtual void OnPluginCreated() {}
	// End of UCSPluginDescriptor interface
};
