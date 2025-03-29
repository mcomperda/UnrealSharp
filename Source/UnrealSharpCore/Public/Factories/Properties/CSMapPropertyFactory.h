#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSMapPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMapPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Map; }
	virtual FFieldClass* GetPropertyClass() override { return FMapProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
