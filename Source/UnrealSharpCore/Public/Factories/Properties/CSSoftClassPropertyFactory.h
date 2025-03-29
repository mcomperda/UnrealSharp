#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSSoftClassPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSoftClassPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::SoftClass; }
	virtual FFieldClass* GetPropertyClass() override { return FSoftClassProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
