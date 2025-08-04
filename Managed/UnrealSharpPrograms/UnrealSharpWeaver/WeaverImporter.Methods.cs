using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace UnrealSharpWeaver;

public partial class WeaverImporter
{
    public static readonly EFunctionFlags RpcFlags = EFunctionFlags.NetServer | EFunctionFlags.NetClient | EFunctionFlags.NetMulticast;
    public static readonly string UFunctionAttribute = "UFunctionAttribute";

    public MethodReference ImportMethod(MethodReference method)
    {
        return UserAssembly.MainModule.ImportReference(method);
    }

    /// <param name="name">name the method copy will have</param>
    /// <param name="method">original method</param>
    /// <param name="addMethod">Add the method copy to the declaring type. this allows to use the original sources to be matched to the copy.</param>
    /// <param name="copyMetadataToken"></param>
    /// <returns>new instance of as copy of the original</returns>
    public MethodDefinition CopyMethod(string name, MethodDefinition method, bool addMethod = true, bool copyMetadataToken = true)
    {
        MethodDefinition newMethod = new MethodDefinition(name, method.Attributes, method.ReturnType)
        {
            HasThis = true,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
            Body = method.Body
        };

        if (copyMetadataToken)
        {
            newMethod.MetadataToken = method.MetadataToken;
        }

        foreach (ParameterDefinition parameter in method.Parameters)
        {
            TypeReference importedType = ImportType(parameter.ParameterType);
            newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, importedType));
        }

        if (addMethod)
        {
            method.DeclaringType.Methods.Add(newMethod);
        }

        return newMethod;
    }

}
