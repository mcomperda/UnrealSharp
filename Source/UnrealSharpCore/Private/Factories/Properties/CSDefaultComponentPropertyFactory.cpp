#include "Factories/Properties/CSDefaultComponentPropertyFactory.h"

#include "Factories/Properties/CSObjectPropertyFactory.h"
#include "MetaData/CSDefaultComponentMetaData.h"

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}

FProperty* UCSDefaultComponentPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UCSObjectPropertyFactory* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyFactory>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyMetaData);
}
