#include "Factories/Properties/CSSoftClassPropertyFactory.h"

#include "MetaData/CSObjectMetaData.h"

struct FCSObjectMetaData;

FProperty* UCSSoftClassPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSoftClassProperty* NewProperty = static_cast<FSoftClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = ObjectMetaData->InnerType.GetOwningClass();
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSSoftClassPropertyFactory::CreateTypeMetaData(
	ECSPropertyType PropertyType)
{
	return MakeShared<FCSObjectMetaData>();
}
