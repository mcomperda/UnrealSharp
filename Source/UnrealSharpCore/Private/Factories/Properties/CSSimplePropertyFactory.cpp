#include "Factories/Properties/CSSimplePropertyFactory.h"

#include "MetaData/CSPropertyType.h"

UCSSimplePropertyFactory::UCSSimplePropertyFactory(FObjectInitializer const& ObjectInitializer): Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Bool, FBoolProperty::StaticClass() },
		{ ECSPropertyType::Int8, FInt8Property::StaticClass() },
		{ ECSPropertyType::Int16, FInt16Property::StaticClass() },
		{ ECSPropertyType::Int, FIntProperty::StaticClass() },
		{ ECSPropertyType::Int64, FInt64Property::StaticClass() },
		{ ECSPropertyType::Byte, FByteProperty::StaticClass() },
		{ ECSPropertyType::UInt16, FUInt16Property::StaticClass() },
		{ ECSPropertyType::UInt32, FUInt32Property::StaticClass() },
		{ ECSPropertyType::UInt64, FUInt64Property::StaticClass() },
		{ ECSPropertyType::Double, FDoubleProperty::StaticClass() },
		{ ECSPropertyType::Float, FFloatProperty::StaticClass() },
		{ ECSPropertyType::String, FStrProperty::StaticClass() },
		{ ECSPropertyType::Name, FNameProperty::StaticClass() },
		{ ECSPropertyType::Text, FTextProperty::StaticClass() },
	};
}

bool UCSSimplePropertyFactory::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	return TypeToFieldClass.Contains(InPropertyType);
}

FFieldClass* UCSSimplePropertyFactory::GetPropertyClass()
{
	return TypeToFieldClass[GetPropertyType()];
}