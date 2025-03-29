#pragma once

#include "CSTypeInfo.h"
#include "ManagedTypeBuilders/CSGeneratedStructBuilder.h"
#include "ManagedTypes/CSScriptStruct.h"
#include "MetaData/CSStructMetaData.h"

struct UNREALSHARPCORE_API FCSharpStructInfo : TCSharpTypeInfo<FCSStructMetaData, UCSScriptStruct, FCSGeneratedStructBuilder>
{
	FCSharpStructInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly) {}
	FCSharpStructInfo() {};
};
