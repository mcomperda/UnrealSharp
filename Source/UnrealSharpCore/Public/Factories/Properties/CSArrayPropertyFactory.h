#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "MetaData/CSPropertyType.h"
#include "CSArrayPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSArrayPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyFactory interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Array; }
	virtual FFieldClass* GetPropertyClass() override { return FArrayProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
	
};
