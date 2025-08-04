﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataMapType : NativeDataContainerType
{
    public PropertyMetaData ValueProperty { get; set; }
    
    public NativeDataMapType(WeaverImporter importer, TypeReference typeRef, int arrayDim, TypeReference key, TypeReference value)
        : base(importer, typeRef, arrayDim, PropertyType.Map, key)
    {
        ValueProperty = PropertyMetaData.FromTypeReference(_importer, value, "Value");
        IsNetworkSupported = false;
    }

    public override string GetContainerMarshallerName()
    {
        return "MapMarshaller`2";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "MapCopyMarshaller`2";
    }

    public override void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        base.EmitDynamicArrayMarshallerDelegates(processor, type);
        ValueProperty.PropertyDataType.EmitDynamicArrayMarshallerDelegates(processor, type);
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        ValueProperty.PropertyDataType.PrepareForRewrite(typeDefinition, propertyMetadata, "");
    }

    public override void InitializeMarshallerParameters()
    {
        ContainerMarshallerTypeParameters =
        [
            _importer.ImportType(InnerProperty.PropertyDataType.CSharpType),
            _importer.ImportType(ValueProperty.PropertyDataType.CSharpType)
        ];
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.IDictionary`2";
    }
}