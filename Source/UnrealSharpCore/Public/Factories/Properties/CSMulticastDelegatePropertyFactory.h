#pragma once

#include "CoreMinimal.h"
#include "CSDelegateBasePropertyFactory.h"
#include "CSMulticastDelegatePropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMulticastDelegatePropertyFactory : public UCSDelegateBasePropertyFactory
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::MulticastInlineDelegate; }
	virtual FFieldClass* GetPropertyClass() override { return FMulticastInlineDelegateProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
	
};
