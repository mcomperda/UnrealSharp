#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSStructPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSStructPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()

	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Struct; }
	virtual FFieldClass* GetPropertyClass() override { return FStructProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
