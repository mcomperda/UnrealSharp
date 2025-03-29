#pragma once

#include "CSTypeInfo.h"
#include "ManagedTypeBuilders/CSGeneratedEnumBuilder.h"
#include "ManagedTypes/CSEnum.h"
#include "MetaData/CSEnumMetaData.h"

struct UNREALSHARPCORE_API FCSharpEnumInfo : TCSharpTypeInfo<FCSEnumMetaData, UCSEnum, FCSGeneratedEnumBuilder>
{
	FCSharpEnumInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly) {}
	FCSharpEnumInfo() {};
};
