#pragma once

#include "CoreMinimal.h"
#include "CSPropertyFactory.h"
#include "CSCommonPropertyFactory.generated.h"

#define REGISTER_METADATA_WITH_NAME(CustomName, MetaDataName) \
MetaDataFactoryMap.Add(CustomName, \
[]() \
{ \
return MakeShared<MetaDataName>(); \
});

#define REGISTER_METADATA(PropertyName, MetaDataName) \
REGISTER_METADATA_WITH_NAME(PropertyName, MetaDataName)

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSCommonPropertyFactory : public UCSPropertyFactory
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyFactory interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyFactory interface
	
	TMap<ECSPropertyType, FFieldClass*> TypeToFieldClass;
	TMap<ECSPropertyType, TFunction<TSharedPtr<FCSUnrealType>()>> MetaDataFactoryMap;
};
