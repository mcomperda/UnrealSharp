#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSEnumPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSEnumPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Enum; }
	virtual FFieldClass* GetPropertyClass() override { return FEnumProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
