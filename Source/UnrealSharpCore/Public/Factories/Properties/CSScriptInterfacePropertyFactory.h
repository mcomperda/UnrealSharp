#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSScriptInterfacePropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSScriptInterfacePropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::ScriptInterface; }
	virtual FFieldClass* GetPropertyClass() override { return FInterfaceProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
};
