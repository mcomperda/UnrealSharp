#include "Factories/Properties/CSArrayPropertyFactory.h"

#include "Factories/Properties/CSPropertyFactory.h"
#include "Factories/Properties/CSPropertyFactoryUtils.h"
#include "MetaData/CSContainerBaseMetaData.h"

FProperty* UCSArrayPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FArrayProperty* NewProperty = static_cast<FArrayProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	NewProperty->Inner = FCSPropertyFactoryUtils::CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	NewProperty->Inner->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSArrayPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}