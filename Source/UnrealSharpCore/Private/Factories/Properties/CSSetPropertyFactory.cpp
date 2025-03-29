#include "Factories/Properties/CSSetPropertyFactory.h"

#include "Factories/Properties/CSPropertyFactoryUtils.h"
#include "MetaData/CSContainerBaseMetaData.h"

FProperty* UCSSetPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSetProperty* ArrayProperty = static_cast<FSetProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	ArrayProperty->ElementProp = FCSPropertyFactoryUtils::CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	ArrayProperty->ElementProp->Owner = ArrayProperty;
	return ArrayProperty;
}

TSharedPtr<FCSUnrealType> UCSSetPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}
