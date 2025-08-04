using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver.Utilities;

public static class TypeDefinitionUtilities
{
    public static readonly string UClassCallbacks = "UClassExporter";
    public static readonly string UClassAttribute = "UClassAttribute";
    
    public static readonly string UEnumAttribute = "UEnumAttribute";
    public static readonly string UStructAttribute = "UStructAttribute";
    public static readonly string UInterfaceAttribute = "UInterfaceAttribute";
    public static readonly string BlittableTypeAttribute = "BlittableTypeAttribute";
    
    public static CustomAttribute? GetUClass(this IMemberDefinition definition)
    {
        return definition.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UClassAttribute);
    }
    
    public static bool IsUClass(this IMemberDefinition definition)
    {
        return GetUClass(definition) != null;
    }
    
    public static bool IsUInterface(this TypeDefinition typeDefinition)
    {
        return GetUInterface(typeDefinition) != null;
    }
    
    public static bool IsUEnum(this TypeDefinition typeDefinition)
    {
        return GetUEnum(typeDefinition) != null;
    }
    
    public static CustomAttribute? GetUStruct(this IMemberDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UStructAttribute);
    }
    
    public static bool IsUStruct(this IMemberDefinition definition)
    {
        return GetUStruct(definition) != null;
    }
    
    public static string GetEngineName(this IMemberDefinition memberDefinition)
    {
        IMemberDefinition currentMemberIteration = memberDefinition;
        while (currentMemberIteration != null)
        {
            CustomAttribute? genTypeAttribute = currentMemberIteration.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.Name == WeaverImporter.GeneratedTypeAttribute);
            
            if (genTypeAttribute is not null)
            {
                return (string) genTypeAttribute.ConstructorArguments[0].Value;
            }

            if (memberDefinition.IsUClass() && memberDefinition.Name.StartsWith('U') ||
                memberDefinition.IsUStruct() && memberDefinition.Name.StartsWith('F'))
            {
                return memberDefinition.Name[1..];
            }
            
            if (currentMemberIteration is MethodDefinition { IsVirtual: true } virtualMethodDefinition)
            {
                if (currentMemberIteration == virtualMethodDefinition.GetBaseMethod())
                {
                    break;
                }
                
                currentMemberIteration = virtualMethodDefinition.GetBaseMethod();
            }
            else
            {
                break;
            }
        }
        
        // Same name in engine as in managed code
        return memberDefinition.Name;
    }
    
    public static CustomAttribute? GetUEnum(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UEnumAttribute);
    }
    
    public static CustomAttribute? GetBlittableType(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpCoreAttributesNamespace, BlittableTypeAttribute);
    }
    
    public static bool IsUnmanagedType(this TypeReference typeRef)
    {
        var typeDef = typeRef.Resolve();
    
        // Must be a value type
        if (!typeDef.IsValueType)
            return false;

        // Primitive types and enums are unmanaged
        if (typeDef.IsPrimitive || typeDef.IsEnum)
            return true;

        // For structs, recursively check all fields
        return typeDef.Fields
            .Where(f => !f.IsStatic)
            .Select(f => f.FieldType.Resolve())
            .All(IsUnmanagedType);
    }

    
    public static CustomAttribute? GetUInterface(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UInterfaceAttribute);
    }
    
  
    
    public static PropertyDefinition? FindPropertyByName(this TypeDefinition classOuter, string propertyName)
    {
        foreach (var property in classOuter.Properties)
        {
            if (property.Name == propertyName)
            {
                return property;
            }
        }

        return default;
    }
    
    public static bool IsChildOf(this TypeDefinition type, TypeDefinition parentType)
    {
        TypeDefinition? currentType = type;
        while (currentType != null)
        {
            if (currentType == parentType)
            {
                return true;
            }

            currentType = currentType.BaseType?.Resolve();
        }

        return false;
    }
    
    
    public static MethodDefinition AddMethod(this TypeDefinition type, WeaverImporter importer, string name, TypeReference? returnType, MethodAttributes attributes = MethodAttributes.Private, params TypeReference[] parameterTypes)
    {
        returnType ??= importer.UserAssembly.MainModule.TypeSystem.Void;
        
        var method = new MethodDefinition(name, attributes, returnType);
        
        foreach (var parameterType in parameterTypes)
        {
            method.Parameters.Add(new ParameterDefinition(parameterType));
        }
        type.Methods.Add(method);
        return method;
    }

    private static readonly MethodAttributes MethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
    
    public static MethodDefinition AddToNativeMethod(this TypeDefinition type, TypeDefinition valueType, WeaverImporter importer, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [importer.IntPtrType, importer.Int32TypeRef, valueType];
        }
        
        MethodDefinition toNativeMethod = type.AddMethod(importer, "ToNative", importer.VoidTypeRef, MethodAttributes, parameters);
        return toNativeMethod;
    }
    
    public static MethodDefinition AddFromNativeMethod(this TypeDefinition type, TypeDefinition returnType, WeaverImporter importer, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [importer.IntPtrType, importer.Int32TypeRef];
        }
        
        MethodDefinition fromNative = type.AddMethod(importer, "FromNative", returnType, MethodAttributes, parameters);
        return fromNative;
    }
    
    public static FieldDefinition AddField(this TypeDefinition type, string name, TypeReference typeReference, FieldAttributes attributes = 0)
    {
        if (attributes == 0)
        {
            attributes = FieldAttributes.Static | FieldAttributes.Private;
        }
        
        var field = new FieldDefinition(name, attributes, typeReference);
        type.Fields.Add(field);
        return field;
    }
    
    public static FieldReference FindField(this TypeDefinition typeDef, WeaverImporter importer, string fieldName)
    {
        foreach (var field in typeDef.Fields)
        {
            if (field.Name != fieldName)
            {
                continue;
            }

            return importer.UserAssembly.MainModule.ImportReference(field);
        }
        
        throw new Exception($"{fieldName} not found in {typeDef}.");
    }
    
   
    
  
    public static bool HasMethod(this TypeDefinition typeDef, WeaverImporter importer, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        return FindMethod(typeDef, importer, methodName, throwIfNotFound, parameterTypes) != null;
    }

    public static MethodReference? FindMethod(this TypeReference typeReference, WeaverImporter importer, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        return FindMethod(typeReference.Resolve(), importer, methodName, throwIfNotFound, parameterTypes);
    }

    public static MethodReference? FindMethod(this TypeDefinition typeDef, WeaverImporter importer, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        TypeDefinition? currentClass = typeDef;
        while (currentClass != null)
        {
            MethodReference? method = FindOwnMethod(importer, currentClass, methodName, throwIfNotFound: false, parameterTypes);
            if (method != null)
            {
                return method;
            }

            currentClass = currentClass.BaseType?.Resolve();
        }

        if (throwIfNotFound)
        {
            throw new Exception("Couldn't find method " + methodName + " in " + typeDef + ".");
        }

        return default;
    }
    
    public static MethodReference? FindOwnMethod(WeaverImporter importer, TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        foreach (var classMethod in typeDef.Methods)
        {
            if (classMethod.Name != methodName)
            {
                continue;
            }

            if (parameterTypes.Length > 0 && classMethod.Parameters.Count != parameterTypes.Length)
            {
                continue;
            }

            bool found = true;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (classMethod.Parameters[i].ParameterType.FullName != parameterTypes[i].FullName)
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return classMethod.ImportMethod(importer);
            }
        }

        if (throwIfNotFound)
        {
            throw new Exception("Couldn't find method " + methodName + " in " + typeDef + ".");
        }

        return default;
    }
    
   
    public static string GetMarshallerClassName(this TypeReference typeRef)
    {
        return typeRef.Name + "Marshaller";
    }
    
    public static PropertyType GetPrimitiveTypeCode(this TypeReference type)
    {
        // Is there a better way to do this? The private member e_type on TypeReference has what we want
        return type.FullName switch
        {
            "System.Byte" => PropertyType.Byte,
            "System.SByte" => PropertyType.Int8,
            "System.Int16" => PropertyType.Int16,
            "System.UInt16" => PropertyType.UInt16,
            "System.Int32" => PropertyType.Int,
            "System.UInt32" => PropertyType.UInt32,
            "System.Int64" => PropertyType.Int64,
            "System.UInt64" => PropertyType.UInt64,
            "System.Float" => PropertyType.Float,
            "System.Double" => PropertyType.Double,
            _ => throw new NotImplementedException()
        };
    }
}