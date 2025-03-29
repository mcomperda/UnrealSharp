#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyFactory.h"
#include "CSObjectPropertyFactory.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSObjectPropertyFactory : public UCSCommonPropertyFactory
{
	GENERATED_BODY()
	
public:

	UCSObjectPropertyFactory(FObjectInitializer const& ObjectInitializer);

	// Begin UCSPropertyFactory interface
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyFactory interface
};
