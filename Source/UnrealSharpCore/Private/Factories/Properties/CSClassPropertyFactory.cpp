#include "Factories/Properties/CSClassPropertyFactory.h"

#include "Factories/Properties/CSPropertyFactory.h"
#include "MetaData/CSClassPropertyMetaData.h"
#include "MetaData/CSObjectMetaData.h"

FProperty* UCSClassPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FClassProperty* NewProperty = static_cast<FClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = ObjectMetaData->InnerType.GetOwningClass();
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSClassPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSClassPropertyMetaData>();
}
