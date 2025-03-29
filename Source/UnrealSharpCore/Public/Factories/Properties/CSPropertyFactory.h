#pragma once

#include "CoreMinimal.h"
#include "MetaData/CSPropertyMetaData.h"
#include "MetaData/CSPropertyType.h"
#include "UObject/Object.h"
#include "CSPropertyFactory.generated.h"

struct FCSUnrealType;

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSPropertyFactory : public UObject
{
	GENERATED_BODY()

protected:

	virtual ECSPropertyType GetPropertyType() const;
	virtual FFieldClass* GetPropertyClass();

	static bool CanBeHashed(const FProperty* InParam);

	FProperty* NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass = nullptr);
	
public:

	static FGuid ConstructGUIDFromString(const FString& Name);
	static FGuid ConstructGUIDFromName(const FName& Name);
	
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType);

};
