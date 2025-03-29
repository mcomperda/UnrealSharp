#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSSetPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSetPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Set; }
	virtual FFieldClass* GetPropertyClass() override { return FSetProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
