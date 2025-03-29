#include "Factories/Properties/CSMapPropertyFactory.h"

#include "Factories/Properties/CSPropertyFactoryUtils.h"
#include "MetaData/CSMapPropertyMetaData.h"

FProperty* UCSMapPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMapProperty* NewProperty = static_cast<FMapProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSMapPropertyMetaData> MapPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSMapPropertyMetaData>();
	NewProperty->KeyProp = FCSPropertyFactoryUtils::CreateProperty(Outer, MapPropertyMetaData->InnerProperty);

	if (!CanBeHashed(NewProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyMetaData.Name.ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	NewProperty->KeyProp->Owner = NewProperty;
	NewProperty->ValueProp = FCSPropertyFactoryUtils::CreateProperty(Outer, MapPropertyMetaData->ValueType);
	NewProperty->ValueProp->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSMapPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSMapPropertyMetaData>();
}
