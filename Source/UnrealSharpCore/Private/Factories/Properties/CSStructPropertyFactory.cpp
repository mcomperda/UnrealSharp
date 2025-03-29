#include "Factories/Properties/CSStructPropertyFactory.h"
#include "MetaData/CSStructPropertyMetaData.h"

FProperty* UCSStructPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FStructProperty* StructProperty = static_cast<FStructProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	
	StructProperty->Struct = StructPropertyMetaData->TypeRef.GetOwningStruct();
	
	ensureAlways(StructProperty->Struct);
	return StructProperty;
}

TSharedPtr<FCSUnrealType> UCSStructPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSStructPropertyMetaData>();
}
