using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType : NativeDataBaseDelegateType
{
    public NativeDataDelegateType(WeaverImporter importer, TypeReference type) : base(importer, type, "SingleDelegateMarshaller`1", PropertyType.Delegate)
    {

    }
    
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, Instruction loadNativePointer, Instruction setNativePointer)
    {
        if (!Signature.HasParameters)
        {
            return;
        }
        
        if (propertyMetadata.MemberRef is not PropertyDefinition)
        {
            VariableDefinition propertyPointer = processor.Body.Method.AddLocalVariable(_importer.IntPtrType);
            processor.Append(loadNativePointer);
            processor.Emit(OpCodes.Ldstr, propertyMetadata.Name);
            processor.Emit(OpCodes.Call, _importer.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stloc, propertyPointer);
            processor.Emit(OpCodes.Ldloc, propertyPointer);
        }
        else
        {
            processor.Append(loadNativePointer);
        }
        
        MethodReference initialize = _importer.UnrealDelegateProcessor.FindOrCreateInitializeDelegate(wrapperType.Resolve());
        processor.Emit(OpCodes.Call, initialize);
    }
}