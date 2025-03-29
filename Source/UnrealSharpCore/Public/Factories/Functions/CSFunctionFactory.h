#pragma once

#include "ManagedTypes/Functions/CSFunction.h"
#include "MetaData/CSClassMetaData.h"
#include "MetaData/CSFunctionMetaData.h"

class UNREALSHARPCORE_API FCSFunctionFactory
{
public:
	
	static UCSFunctionBase* CreateFunctionFromMetaData(UClass* Outer, const FCSFunctionMetaData& FunctionMetaData);
	static UCSFunctionBase* CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction);
	
	static void GetOverriddenFunctions(const UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions);
	static void GenerateVirtualFunctions(UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData);
	static void GenerateFunctions(UClass* Outer, const TArray<FCSFunctionMetaData>& FunctionsMetaData);

	static void AddFunctionToOuter(UClass* Outer, UCSFunctionBase* Function);

	static UCSFunctionBase* CreateFunction(
		UClass* Outer,
		const FName& Name,
		const FCSFunctionMetaData& FunctionMetaData,
		EFunctionFlags FunctionFlags = FUNC_None,
		UStruct* ParentFunction = nullptr);
	static void FinalizeFunctionSetup(UClass* Outer, UCSFunctionBase* Function);

private:

	static FProperty* CreateProperty(UCSFunctionBase* Function, const FCSPropertyMetaData& PropertyMetaData);
	
};
