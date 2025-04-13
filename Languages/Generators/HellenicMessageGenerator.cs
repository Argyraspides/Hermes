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

#pragma warning disable CS8600, CS8604, CS8619, CS8618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

public static class HellenicMessageGenerator
{

    private static string OUTPUT_PATH;
    private static string INPUT_HELLENIC_XML_FILE;

    public static string GenerateHellenicMessageFields(XElement messageElement, XElement headerElement)
    {

        StringBuilder stringBuilder = new StringBuilder();

        // <fields> of the message
        XElement fieldsElement = messageElement?.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (fieldsElement == null) stringBuilder.Append($"\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from message !!\n");

        // Each <field> in <fields> of the message
        IEnumerable<XElement> fields = fieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // <fields> of the header
        XElement headerFieldsElement = headerElement?.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (headerFieldsElement == null) stringBuilder.Append($"\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from {HellenicXMLDefinitions.HEADER_ELEMENT} !!\n");

        // Each <field> in <fields> of the header
        IEnumerable<XElement> headerFields = headerFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // List of all attributes values for every single <field> in the header
        IEnumerable<string> headerFieldNameAttributeValues =
            headerFields?.Attributes(HellenicXMLDefinitions.NAME_ATTRIBUTE).Select(attribute => attribute?.Value) ?? Enumerable.Empty<string>();

        // Each <field> element in the <fields> of a message
        foreach (XElement field in fields)
        {
            if (field == null) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                stringBuilder.Append($"\t// WARNING!! Field is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                fieldName = "MISSING_NAME";
            }

            // If the field already exists in the header then we don't need to declare it in the class
            if (field.Attributes().Select(attrib => attrib?.Value).Intersect(headerFieldNameAttributeValues).Any())
            {
                continue;
            }

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldType))
            {
                stringBuilder.Append($"\t// WARNING!! Field '{fieldName}' is missing type attribute !!\n");
                fieldType = $"{HellenicXMLDefinitions.UNKNOWN_TYPE}";
            }

            string csFieldType;
            if (HellenicXMLDefinitions.TypeMap.TryGetValue(fieldType, out string mappedType))
            {
                csFieldType = mappedType;
            }
            else
            {
                stringBuilder.Append($"\t// WARNING!! Unknown field type '{fieldType}' for field '{fieldName}' !!\n");
                csFieldType = HellenicXMLDefinitions.DEFAULT_TYPE;
            }

            XElement descElement = field.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
            string description = descElement?.Value;
            if (string.IsNullOrEmpty(description))
            {
                stringBuilder.Append($"\t// WARNING!! Field '{fieldName}' is missing {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} !!\n");
                description = $"No {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} provided for {fieldName}";
            }

            string descriptionComment = GeneratorUtils.DescriptionToSummaryComment(description);

            stringBuilder.Append($"\n{descriptionComment}\n".Replace("\n", "\n\t"));
            stringBuilder.Append($"public {csFieldType}? {fieldNamePascal} {{ get; set; }}\n\n");

        }

        return stringBuilder.ToString();

    }

    public static string GenerateHellenicMessageDefaultConstructor(XElement messageElement)
    {

        StringBuilder stringBuilder = new StringBuilder();

        if (messageElement == null)
        {
            stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} element is null !!\n");
            return stringBuilder.ToString();
        }

        string hellenicMessageName = messageElement.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
        if (string.IsNullOrEmpty(hellenicMessageName))
        {
            stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
            hellenicMessageName = $"{HellenicXMLDefinitions.UNKNOWN_MESSAGE}";
        }

        string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);
        stringBuilder.Append($"\tpublic {hellenicMessageNamePascal}()\n\t{{\n");

        XElement messageFieldsElement = messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (messageFieldsElement == null)
        {
            stringBuilder.Append($"\t\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from {HellenicXMLDefinitions.MESSAGE_ELEMENT} !!\n");
        }

        IEnumerable<XElement> messageFields = messageFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // Default constructor should initialize all overridden fields
        foreach (XElement field in messageFields)
        {
            if (field == null) continue;

            bool hasOverrideAttribute =
                field.Attributes().Any(attrib => attrib?.Name?.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE);
            if (hasOverrideAttribute)
            {
                string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
                if (string.IsNullOrEmpty(fieldName))
                {
                    stringBuilder.Append($"\t\t// WARNING!! Overridden {HellenicXMLDefinitions.FIELD_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                    continue;
                }

                string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

                string fieldValue = field.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE)?.Value;
                if (string.IsNullOrEmpty(fieldValue))
                {
                    stringBuilder.Append($"\t\t// WARNING!! Overridden {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' is missing {HellenicXMLDefinitions.VALUE_ATTRIBUTE} attribute !!\n");
                    fieldValue = "null";
                }

                stringBuilder.Append($"\t\t{fieldNamePascal} = {fieldValue};\n");
            }

        }
        stringBuilder.Append($"\t{HellenicXMLDefinitions.END_BRACE}\n\n");

        return stringBuilder.ToString();

    }

    private static string GenerateConstructorParameters(XElement headerElement, XElement messageElement)
    {
        StringBuilder paramsBuilder = new StringBuilder();

        if (headerElement == null || messageElement == null)
        {
            paramsBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} or {HellenicXMLDefinitions.MESSAGE_ELEMENT} element is null !!\n");
            return paramsBuilder.ToString();
        }

        // The fields that all messages should have
        XElement headerFieldsElement = headerElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (headerFieldsElement == null)
        {
            paramsBuilder.Append($"\t\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from {HellenicXMLDefinitions.HEADER_ELEMENT} !!\n");
        }

        IEnumerable<XElement> headerFields = headerFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // Fields defined in the <fields> element of the <messages>
        XElement uniqueFieldsElement = messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (uniqueFieldsElement == null)
        {
            paramsBuilder.Append($"\t\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from message !!\n");
        }

        IEnumerable<XElement> uniqueFields = uniqueFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // Generate parameters for the non-overridden header fields
        foreach (XElement field in headerFields)
        {
            if (field == null) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                paramsBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} field is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                continue;
            }

            // All fields in the derived message that have the same name as the header
            IEnumerable<XElement> matchingFields =
                uniqueFields.Where(xe => xe?.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value == fieldName);

            // Of all the matching fields, are any marked as "override"?
            bool fieldIsOverridden =
                matchingFields.Any(xe => xe?.Attributes()
                    .Any(attr => attr?.Name?.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE) == true);

            // If so, the field will have an assigned value already and  so we shouldn't include it in the constructor params
            if(fieldIsOverridden) continue;

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);
            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldType))
            {
                paramsBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' is missing {HellenicXMLDefinitions.TYPE_ATTRIBUTE} attribute !!\n");
                fieldType = HellenicXMLDefinitions.UNKNOWN_TYPE;
            }

            string csFieldType;
            if (HellenicXMLDefinitions.TypeMap.TryGetValue(fieldType, out string mappedType))
            {
                csFieldType = mappedType;
            }
            else
            {
                paramsBuilder.Append($"\t\t// WARNING!! Unknown type '{fieldType}' for {HellenicXMLDefinitions.HEADER_ELEMENT} {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' !!\n");
                csFieldType = HellenicXMLDefinitions.DEFAULT_TYPE;
            }

            paramsBuilder.Append($"\t\t{csFieldType} p{fieldNamePascal},\n");
        }

        // Generate parameters for non-overridden message fields
        foreach (XElement field in uniqueFields)
        {
            if (field == null) continue;

            // If any of the fields are marked as overridden, don't include them in the constructor parameters
            bool fieldOverrides = field.Attributes().Any(attrib =>
                attrib?.Name?.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE);
            if(fieldOverrides) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                paramsBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} {HellenicXMLDefinitions.FIELD_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                continue;
            }

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldType))
            {
                paramsBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' is missing {HellenicXMLDefinitions.TYPE_ATTRIBUTE} attribute !!\n");
                fieldType = HellenicXMLDefinitions.UNKNOWN_TYPE;
            }

            string csFieldType;
            if (HellenicXMLDefinitions.TypeMap.TryGetValue(fieldType, out string mappedType))
            {
                csFieldType = mappedType;
            }
            else
            {
                paramsBuilder.Append($"\t\t// WARNING!! Unknown {HellenicXMLDefinitions.TYPE_ATTRIBUTE} '{fieldType}' for {HellenicXMLDefinitions.MESSAGE_ELEMENT} {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' !!\n");
                csFieldType = HellenicXMLDefinitions.DEFAULT_TYPE;
            }

            paramsBuilder.Append($"\t\t{csFieldType} p{fieldNamePascal},\n");
        }

        // Remove the trailing comma just before the \n
        if (paramsBuilder.Length > 2)
        {
            paramsBuilder.Length -= 2; // Remove trailing ",\n"
        }

        return paramsBuilder.ToString();
    }

    private static string GenerateConstructorBody(XElement headerElement, XElement messageElement)
    {
        StringBuilder bodyBuilder = new StringBuilder();

        if (headerElement == null || messageElement == null)
        {
            bodyBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} or {HellenicXMLDefinitions.MESSAGE_ELEMENT} element is null !!\n");
            return bodyBuilder.ToString();
        }

        // The fields that all messages should have
        XElement headerFieldsElement = headerElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (headerFieldsElement == null)
        {
            bodyBuilder.Append($"\t\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from {HellenicXMLDefinitions.HEADER_ELEMENT} !!\n");
        }

        IEnumerable<XElement> headerFields = headerFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // Fields defined in the <fields> element of the <messages>
        XElement uniqueFieldsElement = messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (uniqueFieldsElement == null)
        {
            bodyBuilder.Append($"\t\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from {HellenicXMLDefinitions.FIELDS_ELEMENT} !!\n");
        }

        IEnumerable<XElement> uniqueFields = uniqueFieldsElement?.Elements(HellenicXMLDefinitions.FIELD_ELEMENT) ?? Enumerable.Empty<XElement>();

        // Assign values for all message fields
        foreach (XElement field in uniqueFields)
        {
            if (field == null) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                bodyBuilder.Append($"\t\t// WARNING!! Message {HellenicXMLDefinitions.FIELD_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                continue;
            }

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            bool isOverriddenFromHeader =
                field.Attributes().Any(attrib => attrib?.Name?.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE);
            if (isOverriddenFromHeader)
            {
                string fieldValue = field.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE)?.Value;
                if (string.IsNullOrEmpty(fieldValue))
                {
                    bodyBuilder.Append($"\t\t// WARNING!! Overridden {HellenicXMLDefinitions.FIELD_ELEMENT} '{fieldName}' is missing {HellenicXMLDefinitions.VALUE_ATTRIBUTE} attribute !!\n");
                    fieldValue = "null";
                }

                bodyBuilder.Append($"\t\t{fieldNamePascal} = {fieldValue};\n");
            }
            else
            {
                bodyBuilder.Append($"\t\t{fieldNamePascal} = p{fieldNamePascal};\n");
            }
        }

        // Assign values for non-overridden header fields
        foreach (XElement field in headerFields)
        {
            if (field == null) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                bodyBuilder.Append($"\t\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} field is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                continue;
            }

            // List of all <field>'s from the derived message whose names match that of the header field
            IEnumerable<XElement> matchingFields =
                uniqueFields.Where(f => f?.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value == fieldName);

            // Of all matching fields from above, are any overridden?
            bool overridden =
                matchingFields.Any(f => f?.Attributes().Any(attr => attr?.Name?.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE) == true);

            if (!overridden)
            {
                string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);
                bodyBuilder.Append($"\t\t{fieldNamePascal} = p{fieldNamePascal};\n");
            }
        }

        return bodyBuilder.ToString();
    }

    private static string GenerateHellenicMessageConstructor(XElement headerElement, XElement messageElement)
    {
        StringBuilder stringBuilder = new StringBuilder();

        if (headerElement == null || messageElement == null)
        {
            stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} or {HellenicXMLDefinitions.MESSAGE_ELEMENT} element is null !!\n");
            return stringBuilder.ToString();
        }

        string hellenicMessageName = messageElement.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
        if (string.IsNullOrEmpty(hellenicMessageName))
        {
            stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
            hellenicMessageName = HellenicXMLDefinitions.UNKNOWN_MESSAGE;
        }

        string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);

        stringBuilder.Append($"\tpublic {hellenicMessageNamePascal}(\n");
        stringBuilder.Append(GenerateConstructorParameters(headerElement, messageElement));

        stringBuilder.Append("\t)\n\t{\n");
        stringBuilder.Append(GenerateConstructorBody(headerElement, messageElement));

        stringBuilder.Append($"\t{HellenicXMLDefinitions.END_BRACE}\n");

        return stringBuilder.ToString();
    }



    private static void GenerateHellenicMessageBaseClass(XElement hellenicHeader)
    {

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_FILE_HEADER);
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_MESSAGE_FUNCTION_DECLARATION);

        if (hellenicHeader == null)
        {
            stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} element is null !!\n");
            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);
            File.WriteAllText($"{OUTPUT_PATH}/{HellenicXMLDefinitions.HELLENIC_CLASS_NAME}.cs", stringBuilder.ToString());
            return;
        }

        // The <fields> element for the header
        XElement fieldsElement = hellenicHeader.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        if (fieldsElement == null)
        {
            stringBuilder.Append($"\t// WARNING!! <{HellenicXMLDefinitions.FIELDS_ELEMENT}> element missing from header !!\n");
        }

        // List of every single <field> element inside <fields>
        IEnumerable<XElement> fields = fieldsElement?.Elements() ?? Enumerable.Empty<XElement>();

        foreach(XElement field in fields)
        {
            if (field == null) continue;

            // E.g., "latitude_longitude" in the XML and "LatitudeLongitude" in C# Hellenic
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldName))
            {
                stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} field is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                fieldName = "MISSING_NAME";
            }

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            // E.g., "uint32_t" in the XML, and "uint" in C# (cs)
            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(fieldType))
            {
                stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} field '{fieldName}' is missing {HellenicXMLDefinitions.TYPE_ATTRIBUTE} attribute !!\n");
                fieldType = HellenicXMLDefinitions.UNKNOWN_TYPE;
            }

            string csFieldType;
            if (HellenicXMLDefinitions.TypeMap.TryGetValue(fieldType, out string mappedType))
            {
                csFieldType = mappedType;
            }
            else
            {
                stringBuilder.Append($"\t// WARNING!! Unknown {HellenicXMLDefinitions.TYPE_ATTRIBUTE} '{fieldType}' for {HellenicXMLDefinitions.HEADER_ELEMENT} field '{fieldName}' !!\n");
                csFieldType = HellenicXMLDefinitions.DEFAULT_TYPE;
            }

            // The <description> element
            XElement fieldDescElement = field.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
            string description = fieldDescElement?.Value;
            if (string.IsNullOrEmpty(description))
            {
                stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.HEADER_ELEMENT} field '{fieldName}' is missing {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} !!\n");
                description = $"No {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} provided for {fieldName}";
            }

            string fieldDescComment = GeneratorUtils.DescriptionToSummaryComment(description);

            stringBuilder.Append($"\n{fieldDescComment}\n".Replace("\n", "\n\t"));
            stringBuilder.Append($"public {csFieldType}? {fieldNamePascal} {{ get; protected set; }}\n\n");

        }

        stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

        File.WriteAllText($"{OUTPUT_PATH}/{HellenicXMLDefinitions.HELLENIC_CLASS_NAME}.cs", stringBuilder.ToString());

    }

    private static void GenerateHellenicMessages(XElement headerElement, XElement messagesElement)
    {
        if (headerElement == null || messagesElement == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/WARNING_MISSING_ELEMENTS.cs", "// WARNING!! Header or messages element is null !!\n");
            return;
        }

        // List of all <message> elements inside the <messages> element
        IEnumerable<XElement> messageElements = messagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT) ?? Enumerable.Empty<XElement>();

        foreach (XElement message in messageElements)
        {
            if (message == null) continue;

            StringBuilder stringBuilder = new StringBuilder();

            // The <description> element inside the <message> element
            XElement hellenicMessageDescriptionElement = message.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
            string hellenicMessageDescription = hellenicMessageDescriptionElement?.Value;
            if (string.IsNullOrEmpty(hellenicMessageDescription))
            {
                stringBuilder.Append($"// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} !!\n");
                hellenicMessageDescription = "No description provided";
            }

            string hellenicMessageDescriptionComment = GeneratorUtils.DescriptionToSummaryComment(hellenicMessageDescription);

            // E.g., "LATITUDE_LONGITUDE"
            string hellenicMessageName = message.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(hellenicMessageName))
            {
                stringBuilder.Append($"// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                hellenicMessageName = HellenicXMLDefinitions.UNKNOWN_MESSAGE;
            }

            string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);

            string HELLENIC_MESSAGE_CLASS_HEADER = $"partial class {hellenicMessageNamePascal} : {HellenicXMLDefinitions.HELLENIC_CLASS_NAME}\n{{\n";

            stringBuilder.Append($"{hellenicMessageDescriptionComment}\n");
            stringBuilder.Append(HELLENIC_MESSAGE_CLASS_HEADER);

            stringBuilder.Append(GenerateHellenicMessageFields(message, headerElement));
            stringBuilder.Append(GenerateHellenicMessageDefaultConstructor(message));
            stringBuilder.Append(GenerateHellenicMessageConstructor(headerElement, message));
            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

            File.WriteAllText($"{OUTPUT_PATH}/{hellenicMessageNamePascal}.cs", stringBuilder.ToString());

        }

    }

    private static void GenerateHellenicEnums(XElement enumsElement)
    {
        if (enumsElement == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/WARNING_MISSING_ENUMS.cs", "// WARNING!! Enums element is null !!\n");
            return;
        }

        IEnumerable<XElement> enums = enumsElement.Elements(HellenicXMLDefinitions.ENUM_ELEMENT) ?? Enumerable.Empty<XElement>();

        foreach (XElement _enum in enums)
        {
            if (_enum == null) continue;

            StringBuilder stringBuilder = new StringBuilder();

            string enumName = _enum.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(enumName))
            {
                stringBuilder.Append($"// WARNING!! {HellenicXMLDefinitions.ENUM_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                enumName = HellenicXMLDefinitions.UNKNOWN_ENUM;
            }

            string enumNamePascal = GeneratorUtils.SnakeToPascal(enumName);

            stringBuilder.Append($"public enum {enumNamePascal}\n{{\n");

            // The <entries> element in the <enum>
            XElement enumEntriesElement = _enum.Element(HellenicXMLDefinitions.ENTRIES_ELEMENT);
            if (enumEntriesElement == null)
            {
                stringBuilder.Append($"\t// WARNING!! <{HellenicXMLDefinitions.ENTRIES_ELEMENT}> element missing from {HellenicXMLDefinitions.ENUM_ELEMENT} !!\n");
            }

            // List of all <entry> elements inside of the <entries> element
            IEnumerable<XElement> enumEntries = enumEntriesElement?.Elements(HellenicXMLDefinitions.ENTRY_ELEMENT) ?? Enumerable.Empty<XElement>();

            bool hasEntries = false;

            foreach (XElement enumEntry in enumEntries)
            {
                if (enumEntry == null) continue;

                hasEntries = true;

                string entryName = enumEntry.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
                if (string.IsNullOrEmpty(entryName))
                {
                    stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.ENUM_ELEMENT} {HellenicXMLDefinitions.ENTRY_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                    entryName = "MISSING_NAME";
                }

                string entryNamePascal = GeneratorUtils.SnakeToPascal(entryName);

                string entryValue = enumEntry.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE)?.Value;
                if (string.IsNullOrEmpty(entryValue))
                {
                    stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.ENUM_ELEMENT} {HellenicXMLDefinitions.ENTRY_ELEMENT} '{entryName}' is missing {HellenicXMLDefinitions.VALUE_ATTRIBUTE} attribute !!\n");
                    entryValue = "0";
                }

                XElement descElement = enumEntry.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
                string entryDescription = descElement?.Value;
                if (string.IsNullOrEmpty(entryDescription))
                {
                    stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.ENUM_ELEMENT} {HellenicXMLDefinitions.ENTRY_ELEMENT} '{entryName}' is missing {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} !!\n");
                    entryDescription = $"No {HellenicXMLDefinitions.DESCRIPTION_ELEMENT} provided for {entryName}";
                }

                string entryDescriptionSummaryComment = GeneratorUtils.DescriptionToSummaryComment(entryDescription);

                stringBuilder.Append($"\n{entryDescriptionSummaryComment}\n".Replace("\n", "\n\t"));
                stringBuilder.Append($"{entryNamePascal} = {entryValue},\n");
            }

            if (hasEntries)
            {
                // Remove the trailing comma just before the \n
                stringBuilder.Remove(stringBuilder.Length - 2, 1);
            }
            else
            {
                stringBuilder.Append($"\t// WARNING!! No {HellenicXMLDefinitions.ENTRIES_ELEMENT} found for enum !!\n");
                stringBuilder.Append("\tUndefined = 0\n");
            }

            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

            File.WriteAllText($"{OUTPUT_PATH}/{enumNamePascal}.cs", stringBuilder.ToString());
        }
    }

    private static void GenerateHellenicMessageEnums(XElement messagesElement)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_MESSAGE_ENUM_DECLARATION);

        if (messagesElement == null)
        {
            stringBuilder.Append("\t// WARNING!! Messages element is null !!\n");
            stringBuilder.Append("\tUndefined = 0\n");
            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);
            File.WriteAllText($"{OUTPUT_PATH}/{HellenicXMLDefinitions.HELLENIC_MESSAGE_ENUM_NAME}.cs", stringBuilder.ToString());
            return;
        }

        // List of all <message> elements inside of <messages>
        IEnumerable<XElement> messages = messagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT) ?? Enumerable.Empty<XElement>();

        bool hasMessages = false;

        foreach (XElement message in messages)
        {
            if (message == null) continue;

            hasMessages = true;

            string messageId = message.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(messageId))
            {
                stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.ID_ATTRIBUTE} attribute !!\n");
                messageId = "0";
            }

            string messageName = message.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(messageName))
            {
                stringBuilder.Append($"\t// WARNING!! {HellenicXMLDefinitions.MESSAGE_ELEMENT} is missing {HellenicXMLDefinitions.NAME_ATTRIBUTE} attribute !!\n");
                messageName = HellenicXMLDefinitions.UNKNOWN_MESSAGE;
            }

            string messageNamePascal = GeneratorUtils.SnakeToPascal(messageName);

            stringBuilder.Append($"\t{messageNamePascal} = {messageId},\n");
        }

        if (hasMessages)
        {
            // Remove trailing comma just before the \n
            stringBuilder.Remove(stringBuilder.Length - 2, 1);
        }
        else
        {
            stringBuilder.Append($"\t// WARNING!! No {HellenicXMLDefinitions.MESSAGES_ELEMENT} found !!\n");
            stringBuilder.Append("\tUndefined = 0\n");
        }

        stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

        File.WriteAllText($"{OUTPUT_PATH}/{HellenicXMLDefinitions.HELLENIC_MESSAGE_ENUM_NAME}.cs", stringBuilder.ToString());
    }

    public static void Generate(string[] args)
    {

        if(args.Length > 2)
        {
            throw new ArgumentException("There are only two arguments: input XML Hellenic file, and an output directory ");
        }
        if(args.Length == 0)
        {
            INPUT_HELLENIC_XML_FILE = $"{Directory.GetCurrentDirectory()}/hellenic.xml";
            OUTPUT_PATH = ".";
        }
        if(args.Length == 2)
        {
            INPUT_HELLENIC_XML_FILE = args[0];
            if(!File.Exists(INPUT_HELLENIC_XML_FILE))
            {
                throw new FileNotFoundException("The hellenic.xml file you input doesn't exist in that location, or at all");
            }

            OUTPUT_PATH = args[1];
            if(!Directory.Exists(OUTPUT_PATH))
            {
                Directory.CreateDirectory(OUTPUT_PATH);
            }
        }

        XDocument hellenicXml;
        try
        {
            hellenicXml = XDocument.Load(INPUT_HELLENIC_XML_FILE);
        }
        catch (Exception ex)
        {
            File.WriteAllText($"{OUTPUT_PATH}/ERROR_LOADING_XML.cs", $"// WARNING!! Failed to load XML: {ex.Message} !!\n");
            return;
        }

        // <hellenic> element
        XElement root = hellenicXml.Element(HellenicXMLDefinitions.ROOT);
        if (root == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/ERROR_MISSING_ROOT.cs", $"// WARNING!! Root element '{HellenicXMLDefinitions.ROOT}' not found in XML !!\n");
            return;
        }

        // <header> element
        XElement headerElement = root.Element(HellenicXMLDefinitions.HEADER_ELEMENT);
        if (headerElement == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/ERROR_MISSING_HEADER.cs", $"// WARNING!! Header element not found in XML !!\n");
        }

        // <enums> element
        XElement enumsElement = root.Element(HellenicXMLDefinitions.ENUMS_ELEMENT);
        if (enumsElement == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/ERROR_MISSING_ENUMS.cs", $"// WARNING!! Enums element not found in XML !!\n");
        }

        // <messages> element
        XElement messagesElement = root.Element(HellenicXMLDefinitions.MESSAGES_ELEMENT);
        if (messagesElement == null)
        {
            File.WriteAllText($"{OUTPUT_PATH}/ERROR_MISSING_MESSAGES.cs", $"// WARNING!! Messages element not found in XML !!\n");
        }

        GenerateHellenicMessageBaseClass(headerElement);
        GenerateHellenicMessages(headerElement, messagesElement);
        GenerateHellenicEnums(enumsElement);
        GenerateHellenicMessageEnums(messagesElement);
    }
}
