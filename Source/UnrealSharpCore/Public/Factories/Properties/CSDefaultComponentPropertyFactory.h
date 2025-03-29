// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSDefaultComponentPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDefaultComponentPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
public:
	// UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::DefaultComponent; }
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End of implementation
};
