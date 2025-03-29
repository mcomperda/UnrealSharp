#include "CSFieldName.h"
#include "UnrealSharpUtils.h"
#include "ManagedTypeBuilders/CSGeneratedClassBuilder.h"

FCSFieldName::FCSFieldName(FName Name, FName Namespace): Name(Name), Namespace(Namespace)
{
}

FCSFieldName::FCSFieldName(UClass* Class)
{
	const UClass* NativeClass = FCSGeneratedClassBuilder::GetFirstNativeClass(Class);
	
	Name = NativeClass->GetFName();
	Namespace = FUnrealSharpUtils::GetNamespace(NativeClass);
}
