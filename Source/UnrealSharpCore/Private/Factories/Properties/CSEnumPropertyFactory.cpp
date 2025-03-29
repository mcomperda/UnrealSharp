#include "Factories/Properties/CSEnumPropertyFactory.h"
#include "CSManager.h"
#include "MetaData/CSEnumPropertyMetaData.h"

FProperty* UCSEnumPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* NewProperty = static_cast<FEnumProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	const TSharedPtr<FCSEnumPropertyMetaData> EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSEnumPropertyMetaData>();

	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindAssembly(EnumPropertyMetaData->InnerProperty.AssemblyName);
	UEnum* Enum = Assembly->FindEnum(EnumPropertyMetaData->InnerProperty.FieldName);
	
	FByteProperty* UnderlyingProp = new FByteProperty(NewProperty, "UnderlyingType", RF_Public);
	
	NewProperty->SetEnum(Enum);
	NewProperty->AddCppProperty(UnderlyingProp);
	
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSEnumPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSEnumPropertyMetaData>();
}
