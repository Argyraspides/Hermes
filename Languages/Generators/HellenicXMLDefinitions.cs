//
//
// 88        88  88888888888  88888888ba   88        88         db         88888888888  ad88888ba  888888888888  88        88   ad88888ba
// 88        88  88           88      "8b  88        88        d88b        88          d8"     "8b      88       88        88  d8"     "8b
// 88        88  88           88      ,8P  88        88       d8'`8b       88          Y8,              88       88        88  Y8,
// 88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88aaaaaaaa88      d8'  `8b      88aaaaa     `Y8aaaaa,        88       88        88  `Y8aaaaa,
// 88""""""""88  88"""""      88""""""'    88""""""""88     d8YaaaaY8b     88"""""       `"""""8b,      88       88        88    `"""""8b,
// 88        88  88           88           88        88    d8""""""""8b    88                  `8b      88       88        88          `8b
// 88        88  88           88           88        88   d8'        `8b   88          Y8a     a8P      88       Y8a.    .a8P  Y8a     a8P
// 88        88  88888888888  88           88        88  d8'          `8b  88888888888  "Y88888P"       88        `"Y8888Y"'    "Y88888P"
//
//

using System.Collections.Generic;

public static class HellenicXMLDefinitions
{

    // ***************************************************************
    //
    // XML element (tag) and attribute names in hellenic.xml
    //
    // ***************************************************************
    public const string ROOT = "hellenic";

    public const string HEADER_ELEMENT = "header";

    public const string ENUMS_ELEMENT = "enums";
    public const string ENUM_ELEMENT = "enum";

    public const string MESSAGES_ELEMENT = "messages";
    public const string MESSAGE_ELEMENT = "message";


    public const string FIELDS_ELEMENT = "fields";
    public const string FIELD_ELEMENT = "field";

    public const string DESCRIPTION_ELEMENT = "description";

    public const string ENTRIES_ELEMENT = "entries";
    public const string ENTRY_ELEMENT = "entry";

    public const string TYPE_ATTRIBUTE = "type";

    public const string NAME_ATTRIBUTE = "name";

    public const string UNITS_ATTRIBUTE = "units";

    public const string ID_ATTRIBUTE = "id";

    public const string VALUE_ATTRIBUTE = "value";

    public const string OVERRIDE_ATTRIBUTE = "override";


    // ***************************************************************
    //
    // Map from the types in XML to C# types
    //
    // ***************************************************************
    public static readonly Dictionary<string, string> TypeMap = new Dictionary<string, string>
    {
        ["uint8_t"] = "byte",
        ["uint16_t"] = "ushort",
        ["uint32_t"] = "uint",
        ["uint64_t"] = "ulong",
        ["int8_t"] = "sbyte",
        ["int16_t"] = "short",
        ["int32_t"] = "int",
        ["int64_t"] = "long",
        ["float"] = "float",
        ["float32"] = "float",
        ["float64"] = "double",
        ["char"] = "char",
        ["string"] = "string"
    };


    // ***************************************************************
    //
    // Hellenic abstract base class & enum declaration
    //
    // ***************************************************************

    public const string HELLENIC_FILE_HEADER = "#nullable enable\n\nusing Godot;\n\n";
    public const string HELLENIC_CLASS_NAME = "HellenicMessage";
    public const string HELLENIC_MESSAGE_FUNCTION_DECLARATION = $"public abstract partial class {HELLENIC_CLASS_NAME} : RefCounted\n{{\n";

    public const string HELLENIC_MESSAGE_ENUM_NAME = "HellenicMessageType";
    public const string HELLENIC_MESSAGE_ENUM_DECLARATION = $"public enum {HELLENIC_MESSAGE_ENUM_NAME}\n{{\n";

    // ***************************************************************
    //
    // Helper strings
    //
    // ***************************************************************

    public const string END_BRACE = "}\n"; // LMAO reminds me of the #define END_BRACE } example in C++ when learning how macros work
    public const string UNKNOWN_MESSAGE = "UnknownMessage";
    public const string UNKNOWN_TYPE = "unknown_type";
    public const string UNKNOWN_ENUM = "UnknownEnum";
    public const string DEFAULT_TYPE = "object";


}
