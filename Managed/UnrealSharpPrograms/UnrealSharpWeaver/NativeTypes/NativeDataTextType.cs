using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataTextType(WeaverImporter importer, TypeReference textType) : NativeDataSimpleType(importer, textType, "TextMarshaller", 1, PropertyType.Text);