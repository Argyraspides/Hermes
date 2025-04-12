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


using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

public static class HellenicMessageGenerator
{

    public static string OUTPUT_PATH;
    public static string INPUT_HELLENIC_XML_FILE;

    public static string GenerateHellenicMessageDefaultConstructor(XElement headerElement, XElement messageElement)
    {

        StringBuilder stringBuilder = new StringBuilder();

        string hellenicMessageName = messageElement.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
        string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);
        stringBuilder.Append($"\tpublic {hellenicMessageNamePascal}()\n\t{{\n");

        XElement hellenicHeaderFieldsElement = headerElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        IEnumerable<XElement> hellenicHeaderFields = hellenicHeaderFieldsElement.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);

        // Default constructor
        foreach (var field in hellenicHeaderFields)
        {
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);
            string fieldValue = messageElement.Attribute(fieldName)?.Value;

            if(fieldValue != null)
            {
                stringBuilder.Append($"\t\t{fieldNamePascal} = {fieldValue};\n");
            }
        }
        stringBuilder.Append($"\t{HellenicXMLDefinitions.END_BRACE}\n\n");

        return stringBuilder.ToString();

    }

    public static string GenerateHellenicMessageFields(XElement messageElement, XElement headerElement)
    {

        StringBuilder stringBuilder = new StringBuilder();

        // <fields> of the message
        XElement fieldsElement = messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        // Each <field> in <fields> of the message
        IEnumerable<XElement> fields = fieldsElement.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);

        // <fields> of the header
        XElement headerFieldsElement = headerElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        // Each <field> in <fields> of the header
        IEnumerable<XElement> headerFields = headerFieldsElement.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);


        // List of all attributes values for every single <field> in the header
        IEnumerable<string> headerFieldNameAttributeValues =
            headerFields.Attributes(HellenicXMLDefinitions.NAME_ATTRIBUTE).Select(attribute => attribute.Value);

        // Each <field> element in the <fields> of a message
        foreach (XElement field in fields)
        {
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;

            // If the field already exists in the header then we don't need to declare it in the class
            if (field.Attributes().Select(attrib => attrib.Value).Intersect(headerFieldNameAttributeValues).Any())
            {
                continue;
            }

            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
            string csFieldType = HellenicXMLDefinitions.TypeMap[fieldType];

            string description = field.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT).Value;
            string descriptionComment = GeneratorUtils.DescriptionToSummaryComment(description);

            stringBuilder.Append($"\n{descriptionComment}\n".Replace("\n", "\n\t"));
            stringBuilder.Append($"public {csFieldType}? {fieldNamePascal} {{ get; set; }}\n\n");

        }

        return stringBuilder.ToString();

    }

    public static string GenerateHellenicMessageConstructor(XElement headerElement, XElement messageElement){

        StringBuilder stringBuilder = new StringBuilder();

        // The fields that all messages should have
        XElement headerFieldsElement = headerElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        IEnumerable<XElement> headerFields = headerFieldsElement.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);

        // Fields defined in the <fields> element of the <messages>. These may or may not override
        // the fields defined in the headers.
        XElement uniqueFieldsElement = messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        IEnumerable<XElement> uniqueFields = uniqueFieldsElement.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);

        // List of all "name" attribute values for every single field of the message
        IEnumerable<string> messageFieldAttributeNames =
            messageElement.Element(HellenicXMLDefinitions.FIELDS_ELEMENT).
                Elements(HellenicXMLDefinitions.FIELD_ELEMENT).Attributes().Select(attrib => (string)attrib);

        string hellenicMessageName = messageElement.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
        string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);
        stringBuilder.Append($"\tpublic {hellenicMessageNamePascal}(\n");

        // Generate the constructor declaration for the mandatory header fields
        foreach (XElement field in headerFields)
        {
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
            string csFieldType = HellenicXMLDefinitions.TypeMap[fieldType];

            // If any of the attributes inside the corresponding message are also inside the header,
            // this means they've been overridden and we won't include them in the constructor
            if(messageFieldAttributeNames.Any(name => name == fieldName)) continue;

            stringBuilder.Append($"\t\t{csFieldType} p{fieldNamePascal},\n");

        }


        // Generate the constructor declaration for the unique fields of this particular message
        foreach (XElement field in uniqueFields)
        {
            bool fieldOverrides = field.Attributes().Any(attrib => attrib.Name.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE);
            if(fieldOverrides) continue;

            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
            string csFieldType = HellenicXMLDefinitions.TypeMap[fieldType];

            stringBuilder.Append($"\t\t{csFieldType} p{fieldNamePascal},\n");

        }

        // Remove the trailing comma just before the \n
        stringBuilder.Remove(stringBuilder.Length - 2, 1);
        stringBuilder.Append("\t)\n\t{\n");

        // Final constructor body -- all the unique fields in the derived message
        foreach (XElement field in uniqueFields)
        {
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            bool isOverridenFromHeader =
                field.Attributes().Any(attrib => attrib.Name.LocalName == HellenicXMLDefinitions.OVERRIDE_ATTRIBUTE);

            if (isOverridenFromHeader)
            {
                string fieldValue = field.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE).Value;
                stringBuilder.Append($"\t\t{fieldNamePascal} = {fieldValue};\n");
            }
            else
            {
                stringBuilder.Append($"\t\t{fieldNamePascal} = p{fieldNamePascal};\n");
            }

        }

        // Final constructor body -- all the header fields that haven't yet been added (non-overridden)
        foreach (XElement field in headerFields)
        {
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            // In the last for loop we already initialized fields in the constructor body
            // that were overridden from the derived message. Here we add header fields that
            // aren't overridden.
            if (!messageFieldAttributeNames.Any(name => name == fieldName))
            {
                stringBuilder.Append($"\t\t{fieldNamePascal} = p{fieldNamePascal};\n");
            }
        }


        stringBuilder.Append($"\t{HellenicXMLDefinitions.END_BRACE}\n");

        return stringBuilder.ToString();

    }



    public static void GenerateHellenicMessageBaseClass(XElement hellenicHeader)
    {

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_FILE_HEADER);
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_MESSAGE_FUNCTION_DECLARATION);

        // The <fields> element for the header
        XElement fieldsElement = hellenicHeader.Element(HellenicXMLDefinitions.FIELDS_ELEMENT);
        // List of every single <field> element inside <fields>
        IEnumerable<XElement> fields = fieldsElement.Elements();

        foreach(XElement field in fields)
        {

            // E.g., "latitude_longitude" in the XML and "LatitudeLongitude" in C# Hellenic
            string fieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string fieldNamePascal = GeneratorUtils.SnakeToPascal(fieldName);

            // E.g., "uint32_t" in the XML, and "uint" in C# (cs)
            string fieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
            string csFieldType = HellenicXMLDefinitions.TypeMap[fieldType];

            XElement fieldDescElement = field.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
            string fieldDescComment = GeneratorUtils.DescriptionToSummaryComment(fieldDescElement.Value);

            stringBuilder.Append($"\n{fieldDescComment}\n".Replace("\n", "\n\t"));
            stringBuilder.Append($"public {csFieldType}? {fieldNamePascal} {{ get; protected set; }}\n\n");

        }

        stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

        File.WriteAllText($"{OUTPUT_PATH}/HellenicMessage.cs", stringBuilder.ToString());

    }

    public static void GenerateHellenicMessages(XElement headerElement, XElement messagesElement)
    {

        IEnumerable<XElement> messageElements = messagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT);

        foreach (XElement message in messageElements)
        {

            StringBuilder stringBuilder = new StringBuilder();

            XElement hellenicMessageDescriptionElement = message.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
            string hellenicMessageDescription = hellenicMessageDescriptionElement.Value;
            string hellenicMessageDescriptionComment = GeneratorUtils.DescriptionToSummaryComment(hellenicMessageDescription);

            string hellenicMessageName = message.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string hellenicMessageNamePascal = GeneratorUtils.SnakeToPascal(hellenicMessageName);

            string HELLENIC_MESSAGE_CLASS_HEADER = $"partial class {hellenicMessageNamePascal} : {HellenicXMLDefinitions.HELLENIC_CLASS_NAME}\n{{\n";

            stringBuilder.Append($"{hellenicMessageDescriptionComment}\n");
            stringBuilder.Append(HELLENIC_MESSAGE_CLASS_HEADER);


            IEnumerable<XElement> messageFields = message.Elements(HellenicXMLDefinitions.FIELD_ELEMENT);

            foreach (XElement field in messageFields)
            {
                string hellenicFieldType = field.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
                string hellenicCsFieldType = HellenicXMLDefinitions.TypeMap[hellenicFieldType];

                string hellenicFieldName = field.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;

                XElement hellenicFieldDescription = field.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT);
                string hellenicFieldComment = GeneratorUtils.DescriptionToSummaryComment(hellenicFieldDescription.Value);

                stringBuilder.Append($"{hellenicFieldComment}\n");
                stringBuilder.Append($"public {hellenicCsFieldType}? {hellenicFieldName} {{ get; set; }};\n\n");

            }

            stringBuilder.Append(GenerateHellenicMessageFields(message, headerElement));
            stringBuilder.Append(GenerateHellenicMessageDefaultConstructor(headerElement, message));
            stringBuilder.Append(GenerateHellenicMessageConstructor(headerElement, message));
            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

            File.WriteAllText($"{OUTPUT_PATH}/{hellenicMessageNamePascal}.cs", stringBuilder.ToString());

        }

    }

    public static void GenerateHellenicEnums(XElement enumsElement)
    {

        IEnumerable<XElement> enums = enumsElement.Elements(HellenicXMLDefinitions.ENUM_ELEMENT);

        foreach (XElement _enum in enums)
        {

            StringBuilder stringBuilder = new StringBuilder();

            string enumName = _enum.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string enumNamePascal = GeneratorUtils.SnakeToPascal(enumName);

            stringBuilder.Append($"public enum {enumNamePascal}\n{{\n");

            XElement enumEntriesElement = _enum.Element(HellenicXMLDefinitions.ENTRIES_ELEMENT);
            IEnumerable<XElement> enumEntries = enumEntriesElement.Elements(HellenicXMLDefinitions.ENTRY_ELEMENT);

            foreach (XElement enumEntry in enumEntries)
            {
                string entryName = enumEntry.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
                string entryNamePascal = GeneratorUtils.SnakeToPascal(entryName);

                string entryValue = enumEntry.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE).Value;

                string entryDescription = enumEntry.Element(HellenicXMLDefinitions.DESCRIPTION_ELEMENT).Value;
                string entryDescriptionSummaryComment = GeneratorUtils.DescriptionToSummaryComment(entryDescription);

                stringBuilder.Append($"\n{entryDescriptionSummaryComment}\n".Replace("\n", "\n\t"));
                stringBuilder.Append($"{entryNamePascal} = {entryValue},\n");

            }

            // Remove the trailing comma just before the \n
            stringBuilder.Remove(stringBuilder.Length - 2, 1);
            stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

            File.WriteAllText($"{OUTPUT_PATH}/{enumNamePascal}.cs", stringBuilder.ToString());

        }

    }

    public static void GenerateHellenicMessageEnums(XElement messagesElement)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(HellenicXMLDefinitions.HELLENIC_MESSAGE_ENUM_DECLARATION);

        IEnumerable<XElement> messages = messagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT);
        foreach (XElement message in messages)
        {
            string messageId = message.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE).Value;

            string messageName = message.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string messageNamePascal = GeneratorUtils.SnakeToPascal(messageName);

            stringBuilder.Append($"\t{messageNamePascal} = {messageId},\n");

        }

        // Remove trailing comma just before the \n
        stringBuilder.Remove(stringBuilder.Length - 2, 1);
        stringBuilder.Append(HellenicXMLDefinitions.END_BRACE);

        File.WriteAllText($"{OUTPUT_PATH}/{HellenicXMLDefinitions.HELLENIC_MESSAGE_ENUM_NAME}.cs", stringBuilder.ToString());

    }

    static void Main(string[] args)
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

        XDocument mavXml = XDocument.Load(INPUT_HELLENIC_XML_FILE);

        XElement root = mavXml.Element(HellenicXMLDefinitions.ROOT);

        XElement headerElement = root.Element(HellenicXMLDefinitions.HEADER_ELEMENT);

        XElement enumsElement = root.Element(HellenicXMLDefinitions.ENUMS_ELEMENT);

        XElement messagesElement = root.Element(HellenicXMLDefinitions.MESSAGES_ELEMENT);

        GenerateHellenicMessageBaseClass(headerElement);
        GenerateHellenicMessages(headerElement, messagesElement);
        GenerateHellenicEnums(enumsElement);
        GenerateHellenicMessageEnums(messagesElement);
    }
}
