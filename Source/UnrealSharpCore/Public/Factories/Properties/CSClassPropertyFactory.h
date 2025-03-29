// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSClassPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSClassPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Class; }
	virtual FFieldClass* GetPropertyClass() override { return FClassProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
