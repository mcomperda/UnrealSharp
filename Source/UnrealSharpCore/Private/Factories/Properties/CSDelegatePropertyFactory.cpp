#include "Factories/Properties/CSDelegatePropertyFactory.h"
#include "Factories/Functions/CSFunctionFactory.h"
#include "MetaData/CSDelegateMetaData.h"

FProperty* UCSDelegatePropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* NewProperty = static_cast<FDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSDelegateMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	NewProperty->SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Outer->GetOwnerClass(), DelegateMetaData->SignatureFunction);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSDelegatePropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}