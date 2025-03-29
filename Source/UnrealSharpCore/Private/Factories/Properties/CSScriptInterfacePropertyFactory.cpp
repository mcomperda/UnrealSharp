#include "Factories/Properties/CSScriptInterfacePropertyFactory.h"

#include "MetaData/CSObjectMetaData.h"

FProperty* UCSScriptInterfacePropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FInterfaceProperty* InterfaceProperty = static_cast<FInterfaceProperty*>(UCSPropertyFactory::CreateProperty(Outer, PropertyMetaData));
	
	TSharedPtr<FCSObjectMetaData> InterfaceData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	InterfaceProperty->SetInterfaceClass(InterfaceData->InnerType.GetOwningInterface());
	
	return InterfaceProperty;
}

TSharedPtr<FCSUnrealType> UCSScriptInterfacePropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSObjectMetaData>();
}
