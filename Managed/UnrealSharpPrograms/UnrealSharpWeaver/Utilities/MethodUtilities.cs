using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public static class MethodUtilities
{
    public static VariableDefinition AddLocalVariable(this MethodDefinition method, TypeReference typeReference)
    {
        var variable = new VariableDefinition(typeReference);
        method.Body.Variables.Add(variable);
        method.Body.InitLocals = true;
        return variable;
    }
    public static void FinalizeMethod(this MethodDefinition method)
    {
        method.Body.GetILProcessor().Emit(OpCodes.Ret);
        OptimizeMethod(method);
    }

    public static bool ReturnsVoid(this MethodDefinition method)
    {
        return method.ReturnType == method.Module.TypeSystem.Void;
    }
    
    public static bool ReturnsVoid(this MethodReference method)
    {
        return ReturnsVoid(method.Resolve());
    }

    public static bool MethodIsCompilerGenerated(this ICustomAttributeProvider method)
    {
        return method.CustomAttributes.FindAttributeByType("System.Runtime.CompilerServices", "CompilerGeneratedAttribute") != null;
    }

    public static EFunctionFlags GetFunctionFlags(this MethodDefinition method)
    {
        EFunctionFlags flags = (EFunctionFlags)BaseMetaData.GetFlags(method, "FunctionFlagsMapAttribute");

        if (method.IsPublic)
        {
            flags |= EFunctionFlags.Public;
        }
        else if (method.IsFamily)
        {
            flags |= EFunctionFlags.Protected;
        }
        else
        {
            flags |= EFunctionFlags.Private;
        }

        if (method.IsStatic)
        {
            flags |= EFunctionFlags.Static;
        }

        if (flags.HasAnyFlags(WeaverImporter.RpcFlags))
        {
            flags |= EFunctionFlags.Net;

            if (!method.ReturnsVoid())
            {
                throw new InvalidUnrealFunctionException(method, "RPCs can't have return values.");
            }

            if (flags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
            {
                throw new InvalidUnrealFunctionException(method, "BlueprintEvents methods cannot be replicated!");
            }
        }

        // This represents both BlueprintNativeEvent and BlueprintImplementableEvent
        if (flags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
        {
            flags |= EFunctionFlags.Event;
        }

        // Native is needed to bind the function pointer of the UFunction to our own invoke in UE.
        return flags | EFunctionFlags.Native;
    }

    public static void OptimizeMethod(this MethodDefinition method)
    {
        if (method.Body.CodeSize == 0)
        {
            return;
        }

        method.Body.Optimize();
        method.Body.SimplifyMacros();
    }

    public static void RemoveReturnInstruction(this MethodDefinition method)
    {
        if (method.Body.Instructions.Count > 0 && method.Body.Instructions[^1].OpCode == OpCodes.Ret)
        {
            method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
        }
    }

    public static MethodReference ImportMethod(this MethodReference method, WeaverImporter importer)
    {
        return importer.UserAssembly.MainModule.ImportReference(method);
    }
    public static CustomAttribute? GetUFunction(this MethodDefinition function)
    {
        return function.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, WeaverImporter.UFunctionAttribute);
    }

    public static bool IsUFunction(this MethodDefinition method)
    {
        return GetUFunction(method) != null;
    }


}