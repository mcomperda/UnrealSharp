#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyFactory.h"
#include "CSSimplePropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSimplePropertyFactory : public UCSCommonPropertyFactory
{
	GENERATED_BODY()

public:

	UCSSimplePropertyFactory(FObjectInitializer const& ObjectInitializer);

protected:

	// Begin UCSPropertyFactory interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Unknown; }
	virtual FFieldClass* GetPropertyClass() override;
	// End UCSPropertyFactory interface
};
