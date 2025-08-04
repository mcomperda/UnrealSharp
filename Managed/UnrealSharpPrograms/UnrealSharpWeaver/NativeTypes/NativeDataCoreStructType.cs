﻿using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;
class NativeDataCoreStructType : NativeDataBlittableStructTypeBase
{ 
    public NativeDataCoreStructType(WeaverImporter importer, TypeReference structType, int arrayDim) : base(importer, structType, arrayDim)
    {
        var innerPropertyName = structType.Name switch
        {
            "Quaternion" => "Quat",
            "Vector2" => "Vector2D",
            "Vector3" => "Vector",
            "Matrix4x4" => "Matrix",
            _ => structType.Name
        };

        InnerType = new TypeReferenceMetadata(importer, structType.Resolve())
        {
            Name = innerPropertyName
        };
    }
}