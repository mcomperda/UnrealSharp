#pragma once

#include "MetaData/CSPropertyMetaData.h"
#include "UObject/UnrealType.h"

class UCSPropertyFactory;

class UNREALSHARPCORE_API FCSPropertyFactoryUtils
{
public:

	static void Initialize();

	static UCSPropertyFactory* FindPropertyGenerator(ECSPropertyType PropertyType);
	
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData, const TFunction<void(FProperty*)>& OnPropertyCreated = nullptr);
	
	static TSharedPtr<FCSUnrealType> CreateTypeMetaData(const TSharedPtr<FJsonObject>& PropertyMetaData);

	static void TryAddPropertyAsFieldNotify(const FCSPropertyMetaData& PropertyMetaData, UBlueprintGeneratedClass* Class);

private:
	static TArray<TObjectPtr<UCSPropertyFactory>> PropertyGenerators;
};
