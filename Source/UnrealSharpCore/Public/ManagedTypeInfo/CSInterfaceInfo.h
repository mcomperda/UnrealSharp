#pragma once

#include "MetaData/CSInterfaceMetaData.h"
#include "ManagedTypeInfo/CSTypeInfo.h"
#include "ManagedTypeBuilders/CSGeneratedInterfaceBuilder.h"

struct UNREALSHARPCORE_API FCSharpInterfaceInfo : TCSharpTypeInfo<FCSInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSharpInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly) {}
	FCSharpInterfaceInfo() {};
};

