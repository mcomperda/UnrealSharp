#include "Factories/Properties/CSPropertyFactoryUtils.h"

#include "INotifyFieldValueChanged.h"
#include "UnrealSharpUtils.h"
#include "MetaData/CSMetaDataUtils.h"
#include "Factories/Properties/CSPropertyFactory.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"

TArray<TObjectPtr<UCSPropertyFactory>> FCSPropertyFactoryUtils::PropertyGenerators;

void FCSPropertyFactoryUtils::Initialize()
{
	if (PropertyGenerators.Num() > 0)
	{
		return;
	}
	
	TArray<UCSPropertyFactory*> FoundPropertyGeneratorClasses;
	FUnrealSharpUtils::GetAllCDOsOfClass<UCSPropertyFactory>(FoundPropertyGeneratorClasses);
	
	PropertyGenerators.Reserve(FoundPropertyGeneratorClasses.Num());
	
	for (UCSPropertyFactory* PropertyGenerator : FoundPropertyGeneratorClasses)
	{
		PropertyGenerators.Add(PropertyGenerator);
	}
}

FProperty* FCSPropertyFactoryUtils::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UCSPropertyFactory* PropertyGenerator = FindPropertyGenerator(PropertyMetaData.Type->PropertyType);
	FProperty* NewProperty = PropertyGenerator->CreateProperty(Outer, PropertyMetaData);

	NewProperty->SetPropertyFlags(PropertyMetaData.PropertyFlags);
	NewProperty->SetBlueprintReplicationCondition(PropertyMetaData.LifetimeCondition);

#if WITH_EDITOR
	if (!PropertyMetaData.BlueprintSetter.IsEmpty())
	{
		NewProperty->SetMetaData("BlueprintSetter", *PropertyMetaData.BlueprintSetter);

		if (UFunction* BlueprintSetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintSetter))
		{
			BlueprintSetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
		}
	}

	if (!PropertyMetaData.BlueprintGetter.IsEmpty())
	{
		NewProperty->SetMetaData("BlueprintGetter", *PropertyMetaData.BlueprintGetter);
			
		if (UFunction* BlueprintGetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintGetter))
		{
			BlueprintGetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
		}
	}
#endif

	FCSMetaDataUtils::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);
	
	if (UBlueprintGeneratedClass* OwningClass = Cast<UBlueprintGeneratedClass>(Outer))
	{
		if (NewProperty->HasAnyPropertyFlags(CPF_Net))
		{
			++OwningClass->NumReplicatedProperties;
			
			if (!PropertyMetaData.RepNotifyFunctionName.IsNone())
			{
				NewProperty->RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
			}
		}

		TryAddPropertyAsFieldNotify(PropertyMetaData, OwningClass);
	}

	NewProperty->SetFlags(RF_LoadCompleted);
	return NewProperty;
}

FProperty* FCSPropertyFactoryUtils::CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* Property = CreateProperty(Outer, PropertyMetaData);
	Outer->AddCppProperty(Property);
	return Property;
}

void FCSPropertyFactoryUtils::CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData, const TFunction<void(FProperty*)>& OnPropertyCreated)
{
	for (int32 i = PropertyMetaData.Num() - 1; i >= 0; --i)
	{
		const FCSPropertyMetaData& Property = PropertyMetaData[i];
		FProperty* NewProperty = CreateAndAssignProperty(Outer, Property);

		if (OnPropertyCreated)
		{
			OnPropertyCreated(NewProperty);
		}
	}
}

TSharedPtr<FCSUnrealType> FCSPropertyFactoryUtils::CreateTypeMetaData(const TSharedPtr<FJsonObject>& PropertyMetaData)
{
	const TSharedPtr<FJsonObject>& PropertyTypeObject = PropertyMetaData->GetObjectField(TEXT("PropertyDataType"));
	ECSPropertyType PropertyType = static_cast<ECSPropertyType>(PropertyTypeObject->GetIntegerField(TEXT("PropertyType")));
	
	UCSPropertyFactory* PropertyGenerator = FindPropertyGenerator(PropertyType);
	TSharedPtr<FCSUnrealType> PropertiesMetaData = PropertyGenerator->CreateTypeMetaData(PropertyType);
	
	PropertiesMetaData->SerializeFromJson(PropertyTypeObject);
	return PropertiesMetaData;
}

UCSPropertyFactory* FCSPropertyFactoryUtils::FindPropertyGenerator(ECSPropertyType PropertyType)
{
	for (TObjectPtr<UCSPropertyFactory>& PropertyGenerator : PropertyGenerators)
	{
		if (!PropertyGenerator->SupportsPropertyType(PropertyType))
		{
			continue;
		}

		return PropertyGenerator;
	}
	
	return nullptr;
}

void FCSPropertyFactoryUtils::TryAddPropertyAsFieldNotify(const FCSPropertyMetaData& PropertyMetaData, UBlueprintGeneratedClass* Class)
{
	bool bImplementsInterface = Class->ImplementsInterface(UNotifyFieldValueChanged::StaticClass());
	bool bHasFieldNotifyMetaData = PropertyMetaData.HasMetaData(TEXT("FieldNotify"));
	
	if (!bImplementsInterface || !bHasFieldNotifyMetaData)
	{
		return;
	}
	
	Class->FieldNotifies.Add(FFieldNotificationId(PropertyMetaData.Name));
}



