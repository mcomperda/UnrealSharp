#include "Factories/Properties/CSMulticastDelegatePropertyFactory.h"

#include "Factories/Functions/CSFunctionFactory.h"
#include "ManagedTypes/Functions/CSFunction.h"
#include "MetaData/CSDelegateMetaData.h"

FProperty* UCSMulticastDelegatePropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMulticastInlineDelegateProperty* NewProperty =
		static_cast<FMulticastInlineDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	UClass* Class = CastChecked<UClass>(Outer);
	
	UFunction* SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);
	NewProperty->SignatureFunction = SignatureFunction;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}
