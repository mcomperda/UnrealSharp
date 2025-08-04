using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public partial class WeaverImporter
{
    public TypeReference ImportType(TypeReference type)
    {
        return UserAssembly.MainModule.ImportReference(type);
    }


    public TypeReference? FindGenericType(AssemblyDefinition assembly,  string typeNamespace, string typeName, TypeReference[] typeParameters, bool bThrowOnException = true)
    {
        TypeReference? typeRef = FindType(assembly, typeName, typeNamespace, bThrowOnException);
        return typeRef == null ? null : ImportType(typeRef.Resolve().MakeGenericInstanceType(typeParameters));
    }

    public TypeReference? FindType(AssemblyDefinition assembly, string typeName, string typeNamespace = "", bool throwOnException = true)
    {
        foreach (var module in assembly.Modules)
        {
            var allTypes = module.GetAllTypes().ToArray();
            foreach (var type in allTypes)
            {
                if ((typeNamespace.Length > 0 && type.Namespace != typeNamespace) || type.Name != typeName)
                {
                    continue;
                }

                return ImportType(type);
            }
        }

        if (throwOnException)
        {
            throw new TypeAccessException($"Type \"{typeNamespace}.{typeName}\" not found in userAssembly {assembly.Name}");
        }

        return null;
    }
    public TypeDefinition CreateNewClass(string classNamespace, string className, TypeAttributes attributes, TypeReference? parentClass = null)
    {
        return CreateNewClass(UserAssembly, classNamespace, className, attributes, parentClass);
    }

    public TypeDefinition CreateNewClass(AssemblyDefinition assembly, string classNamespace, string className, TypeAttributes attributes, TypeReference? parentClass = null)
    {
        if (parentClass == null)
        {
            parentClass = assembly.MainModule.TypeSystem.Object;
        }

        TypeDefinition newType = new TypeDefinition(classNamespace, className, attributes, parentClass);
        assembly.MainModule.Types.Add(newType);
        return newType;
    }

    public void AddGeneratedTypeAttribute(TypeDefinition type)
    {
        CustomAttribute attribute = new CustomAttribute(GeneratedTypeCtor);
        string typeName = type.Name.Substring(1);
        string fullTypeName = type.Namespace + "." + typeName;
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(UserAssembly.MainModule.TypeSystem.String, typeName));
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(UserAssembly.MainModule.TypeSystem.String, fullTypeName));

        type.CustomAttributes.Add(attribute);
    }

    public TypeReference FindNestedType(TypeDefinition typeDef, string typeName)
    {
        foreach (var nestedType in typeDef.NestedTypes)
        {
            if (nestedType.Name != typeName)
            {
                continue;
            }

            return UserAssembly.MainModule.ImportReference(nestedType);
        }

        throw new Exception($"{typeName} not found in {typeDef}.");
    }

    public bool IsUObject(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.IsUClass())
        {
            return false;
        }

        while (typeDefinition != null)
        {
            if (typeDefinition.BaseType == null)
            {
                return false;
            }

            if (typeDefinition == UObjectDefinition)
            {
                return true;
            }

            typeDefinition = typeDefinition.BaseType.Resolve();
        }

        return false;
    }
    public void ForEachAssembly(Func<AssemblyDefinition, bool> action)
    {
        List<AssemblyDefinition> assemblies = [UnrealSharpAssembly,
        UnrealSharpCoreAssembly,
        UserAssembly,
        ProjectGlueAssembly];

        assemblies.AddRange(WeavedAssemblies);

        foreach (AssemblyDefinition assembly in assemblies)
        {
            if (!action(assembly))
            {
                return;
            }
        }
    }

    public NativeDataType GetDataType(TypeReference typeRef, string propertyName, Collection<CustomAttribute>? customAttributes = null)
    {
        int arrayDim = 1;
        TypeDefinition typeDef = typeRef.Resolve();
        SequencePoint? sequencePoint = ErrorEmitter.GetSequencePointFromMemberDefinition(typeDef);

        if (customAttributes != null)
        {
            CustomAttribute? propertyAttribute = typeDef.GetUProperty();

            if (propertyAttribute != null)
            {
                CustomAttributeArgument? arrayDimArg = propertyAttribute.FindAttributeField("ArrayDim");

                if (typeRef is GenericInstanceType genericType && genericType.GetElementType().FullName == "UnrealSharp.FixedSizeArrayReadWrite`1")
                {
                    if (arrayDimArg.HasValue)
                    {
                        arrayDim = (int)arrayDimArg.Value.Value;

                        // Unreal doesn't have a separate type for fixed arrays, so we just want to generate the inner UProperty type with an arrayDim.
                        typeRef = genericType.GenericArguments[0];
                        typeDef = typeRef.Resolve();
                    }
                    else
                    {
                        throw new InvalidPropertyException(propertyName, sequencePoint, "Fixed array properties must specify an ArrayDim in their [UProperty] attribute");
                    }
                }
                else if (arrayDimArg.HasValue)
                {
                    throw new InvalidPropertyException(propertyName, sequencePoint, "ArrayDim is only valid for FixedSizeArray properties.");
                }
            }
        }

        switch (typeDef.FullName)
        {
            case "System.Double":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Double);
            case "System.Single":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Float);

            case "System.SByte":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Int8);
            case "System.Int16":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Int16);
            case "System.Int32":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Int);
            case "System.Int64":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Int64);

            case "System.Byte":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.Byte);
            case "System.UInt16":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.UInt16);
            case "System.UInt32":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.UInt32);
            case "System.UInt64":
                return new NativeDataBuiltinType(this, typeRef, arrayDim, PropertyType.UInt64);

            case "System.Boolean":
                return new NativeDataBooleanType(this, typeRef, arrayDim);

            case "System.String":
                return new NativeDataStringType(this, typeRef, arrayDim);

            default:

                if (typeRef.IsGenericInstance || typeRef.IsByReference)
                {
                    GenericInstanceType? instanceType = null;
                    if (typeRef is GenericInstanceType genericInstanceType)
                    {
                        instanceType = genericInstanceType;
                    }
                    if (typeRef is ByReferenceType byReferenceType)
                    {
                        instanceType = byReferenceType.ElementType as GenericInstanceType;
                        typeRef = byReferenceType.ElementType;
                    }

                    if (instanceType != null)
                    {
                        TypeReference[] genericArguments = instanceType.GenericArguments.ToArray();
                        string? genericTypeName = instanceType.ElementType.Name;

                        if (genericTypeName.Contains("TArray`1") || genericTypeName.Contains("List`1"))
                        {
                            return new NativeDataArrayType(this, typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TNativeArray`1") || genericTypeName.Contains("ReadOnlySpan`1"))
                        {
                            return new NativeDataNativeArrayType(this,typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TMap`2") || genericTypeName.Contains("Dictionary`2"))
                        {
                            return new NativeDataMapType(this, typeRef, arrayDim, genericArguments[0], genericArguments[1]);
                        }

                        if (genericTypeName.Contains("TSet`1") || genericTypeName.Contains("HashSet`1"))
                        {
                            return new NativeDataSetType(this,typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TSubclassOf`1"))
                        {
                            return new NativeDataClassType(this, typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TWeakObjectPtr`1"))
                        {
                            return new NativeDataWeakObjectType(this, typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TSoftObjectPtr`1"))
                        {
                            return new NativeDataSoftObjectType(this, typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TSoftClassPtr`1"))
                        {
                            return new NativeDataSoftClassType(this, typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("Option`1"))
                        {
                            return new NativeDataOptionalType(this, typeRef, genericArguments[0], arrayDim);
                        }
                    }
                }

                if (typeDef.IsEnum && typeDef.IsUEnum())
                {
                    CustomAttribute? enumAttribute = typeDef.GetUEnum();

                    if (enumAttribute == null)
                    {
                        throw new InvalidPropertyException(propertyName, sequencePoint, "Enum properties must use an UEnum enum: " + typeRef.FullName);
                    }

                    // TODO: This is just true for properties, not for function parameters they can be int. Need a good way to differentiate.
                    // if (typeDef.GetEnumUnderlyingType().Resolve() != ByteTypeRef.Resolve())
                    // {
                    //     throw new InvalidPropertyException(propertyName, sequencePoint, "Enum's exposed to Blueprints must have an underlying type of System.Byte: " + typeRef.FullName);
                    // }

                    return new NativeDataEnumType(this, typeDef, arrayDim);
                }

                if (typeDef.IsInterface && typeDef.IsUInterface())
                {
                    return new NativeDataInterfaceType(this, typeRef, typeDef.Name + "Marshaller");
                }

                if (typeDef.FullName == "UnrealSharp.FText")
                {
                    return new NativeDataTextType(this, typeDef);
                }

                if (typeDef.FullName == "UnrealSharp.FName")
                {
                    return new NativeDataNameType(this, typeDef, arrayDim);
                }

                if (typeDef.Name == "TMulticastDelegate`1")
                {
                    return new NativeDataMulticastDelegate(this, typeRef);
                }

                if (typeDef.Name == "TDelegate`1")
                {
                    return new NativeDataDelegateType(this, typeRef);
                }

                if (customAttributes != null && NativeDataDefaultComponent.IsDefaultComponent(customAttributes))
                {
                    return new NativeDataDefaultComponent(this, customAttributes, typeDef, arrayDim);
                }

                TypeDefinition? superType = typeDef;
                while (superType != null && superType.FullName != "UnrealSharp.Core.UnrealSharpObject")
                {
                    TypeReference superTypeRef = superType.BaseType;
                    superType = superTypeRef != null ? superTypeRef.Resolve() : null;
                }

                if (superType != null)
                {
                    return new NativeDataObjectType(this, typeRef, typeDef, arrayDim);
                }

                // See if this is a struct
                CustomAttribute? structAttribute = typeDef.GetUStruct();

                if (structAttribute == null)
                {
                    return typeDef.IsUnmanagedType() ? new NativeDataUnmanagedType(this, typeDef, arrayDim) : new NativeDataManagedObjectType(this, typeRef, arrayDim);
                }

                return typeDef.GetBlittableType() != null ? new NativeDataBlittableStructType(this,typeDef, arrayDim) : new NativeDataStructType(this, typeDef, typeDef.GetMarshallerClassName(), arrayDim);
        }
    }

}
