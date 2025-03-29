#pragma once

#include "CoreMinimal.h"
#include "CSDelegateBasePropertyFactory.h"
#include "CSDelegatePropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDelegatePropertyFactory : public UCSDelegateBasePropertyFactory
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Delegate; }
	virtual FFieldClass* GetPropertyClass() override { return FDelegateProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
