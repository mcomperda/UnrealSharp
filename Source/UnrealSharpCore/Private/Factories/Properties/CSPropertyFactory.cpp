#include "Factories/Properties/CSPropertyFactory.h"

#if WITH_EDITOR
#include "Kismet2/BlueprintEditorUtils.h"
#endif

ECSPropertyType UCSPropertyFactory::GetPropertyType() const
{
	return ECSPropertyType::Unknown;
}

FFieldClass* UCSPropertyFactory::GetPropertyClass()
{
	PURE_VIRTUAL();
	return nullptr;
}

FProperty* UCSPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return NewProperty(Outer, PropertyMetaData);
}

bool UCSPropertyFactory::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	ECSPropertyType PropertyType = GetPropertyType();
	check(PropertyType != ECSPropertyType::Unknown);
	return PropertyType == InPropertyType;
}

TSharedPtr<FCSUnrealType> UCSPropertyFactory::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	PURE_VIRTUAL();
	return nullptr;
}

FGuid UCSPropertyFactory::ConstructGUIDFromName(const FName& Name)
{
	return ConstructGUIDFromString(Name.ToString());
}

FGuid UCSPropertyFactory::ConstructGUIDFromString(const FString& Name)
{
	const uint32 BufferLength = Name.Len() * sizeof(Name[0]);
	uint32 HashBuffer[5];
	FSHA1::HashBuffer(*Name, BufferLength, reinterpret_cast<uint8*>(HashBuffer));
	return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]); 
}

bool UCSPropertyFactory::CanBeHashed(const FProperty* InParam)
{
#if WITH_EDITOR
	if(InParam->IsA<FBoolProperty>())
	{
		return false;
	}

	if (InParam->IsA<FTextProperty>())
	{
		return false;
	}
	
	if (const FStructProperty* StructProperty = CastField<FStructProperty>(InParam))
	{
		return FBlueprintEditorUtils::StructHasGetTypeHash(StructProperty->Struct);
	}
#endif
	return true;
}

FProperty* UCSPropertyFactory::NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyMetaData.PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	if (FieldClass == nullptr)
	{
		FieldClass = GetPropertyClass();
	}
	
	FProperty* NewProperty = static_cast<FProperty*>(FieldClass->Construct(Outer, PropertyName, RF_Public));
	NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags;
	return NewProperty;
}


