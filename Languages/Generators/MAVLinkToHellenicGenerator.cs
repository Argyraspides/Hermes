using System.Collections;

namespace Hermes.Languages.Generators;

using System.Text;
using System.Xml.Linq;


public static class MAVLinkToHellenicGenerator
{

    private static string OUTPUT_DIRECTORY = ".";

    private static StringBuilder GenerateTranslationClassHeader()
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.Append(HellenicXMLDefinitions.TRANSLATOR_USING_DIRECTIVES);
        stringBuilder.Append(HellenicXMLDefinitions.TRANSLATOR_CLASS_DECLARATION);

        return stringBuilder;
    }

    private static StringBuilder GenerateTranslationAPI()
    {
        return new StringBuilder(HellenicXMLDefinitions.TRANSLATOR_API_DEFINITION);
    }

    private static StringBuilder GenerateTranslationFunctions(
        XElement conversionsElement,
        XElement hellenicMessagesElement,
        XElement mavlinkMessagesElement
        )
    {

        if (conversionsElement == null || hellenicMessagesElement == null || mavlinkMessagesElement == null)
        {
            throw new ArgumentNullException("conversionsElement, mavlinkMessagesElement, or mavlinkMessagesElement cannot be null");
        }

        StringBuilder stringBuilder = new StringBuilder();

        // List of MAVLink <message>'s to convert in <conversions>
        IEnumerable<XElement> messagesToConvert = conversionsElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT);

        // List of all original MAVLink <message>'s from the original MAVLink xml e.g., common.xml
        IEnumerable<XElement> originalMAVLinkMessages = mavlinkMessagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT);

        // List of all original Hellenic <message>'s from the original Hellenic xml e.g., hellenic.xml
        IEnumerable<XElement> originalHellenicMessages = hellenicMessagesElement.Elements(HellenicXMLDefinitions.MESSAGE_ELEMENT);

        foreach (XElement messageToConvert in messagesToConvert)
        {
            string mavlinkId = messageToConvert.Attribute(HellenicXMLDefinitions.COMMON_ID_ATTRIBUTE)?.Value;
            if (string.IsNullOrEmpty(mavlinkId)); // TODO

            // Grab the original MAVLink message for this mapping
            XElement mavlinkMessageElement =
                originalMAVLinkMessages.Elements().
                    First(msg => msg.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE).Value == mavlinkId); // TODO error handling

            string mavlinkMessageName = mavlinkMessageElement.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
            string mavlinkMessageNamePascal = GeneratorUtils.SnakeToPascal(mavlinkMessageName);

            // Function declaration for this message
            stringBuilder.Append(
                $"public static List<HellenicMessage> {mavlinkMessageNamePascal}ToHellenic({HellenicXMLDefinitions.MAVLINK_MESSAGE_TYPE} {HellenicXMLDefinitions.MAVLINK_MESSAGE_PARAM})\n{{\n"
            );

            // Converting the full MAVLink message to the specific message type that it is
            // E.g., "var mavlinkData = mavlinkMessage.ToStructure<MAVLink.mavlink_global_position_int_t>();"
            stringBuilder.Append(
                $"\t\tvar mavlinkData = {HellenicXMLDefinitions.MAVLINK_MESSAGE_PARAM}.ToStructure<MAVLink.mavlink_{mavlinkMessageName.ToLower()}_t>();\n\n"
            );

            // List of all <mapping> elements in the <message> we want to convert
            IEnumerable<XElement> mappings = messageToConvert.Elements(HellenicXMLDefinitions.MAPPING_ELEMENT);

            // LIST OF ALL <default_value> elements in the <message> we want to convert
            IEnumerable<XElement> defaultValues =
                messagesToConvert.Elements(HellenicXMLDefinitions.DEFAULT_VALUE_ELEMENT);

            // List of all hellenic IDs that the MAVLink message maps to
            IEnumerable<string> hellenicIdsToMap =
                mappings.Attributes(HellenicXMLDefinitions.HELLENIC_ID_ATTRIBUTE).Select(attr => attr.Value).Distinct();

            // List of all hellenic <message>'s that we will be mapping to
            IEnumerable<XElement> hellenicMessagesToMap =
                originalHellenicMessages.Where(msg => hellenicIdsToMap.Contains(msg.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE).Value));

            foreach (XElement messageToMap in hellenicMessagesToMap)
            {
                string messageName = messageToMap.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
                string messageNamePascal = GeneratorUtils.SnakeToPascal(messageName);
                stringBuilder.Append($"\t\t{messageNamePascal}HellenicMessage = new {messageNamePascal}(\n");

                // Map each hellenic message parameter to the corresponding mavlink message parameter
                IEnumerable<XElement> hellenicFields =
                    messageToMap.Element(HellenicXMLDefinitions.FIELDS_ELEMENT).Elements();
                foreach (XElement hellenicField in hellenicFields)
                {
                    string hellenicId = hellenicField.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE).Value;
                    string hellenicFieldName = hellenicField.Attribute(HellenicXMLDefinitions.NAME_ATTRIBUTE).Value;
                    string hellenicFieldNamePascal = GeneratorUtils.SnakeToPascal(hellenicFieldName);

                    // Find the <mapping> element that contains the hellenic field
                    // TODO::ARGYRASPIDES() { Is this a problem? What if we have multiple mavlink messages map to the same field? I guess this is
                    // impossible? }
                    XElement correspondingMapping =
                        mappings.Elements().FirstOrDefault(
                            element =>
                                element.Attribute(HellenicXMLDefinitions.HELLENICE_FIELD_NAME_ATTRIBUTE)?.Value == hellenicFieldName
                                &&
                                element.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE)?.Value == hellenicId
                        );

                    // If theres no corresponding MAVLink mapping, we use the default value
                    if (correspondingMapping == null)
                    {
                        XElement correspondingDefaultValue =
                            defaultValues.Elements().FirstOrDefault(
                                element =>
                                    element.Attribute(HellenicXMLDefinitions.HELLENICE_FIELD_NAME_ATTRIBUTE)?.Value == hellenicFieldName
                                    &&
                                    element.Attribute(HellenicXMLDefinitions.ID_ATTRIBUTE)?.Value == hellenicId
                            );

                        string defaultValue = correspondingMapping.Attribute(HellenicXMLDefinitions.VALUE_ATTRIBUTE).Value;
                        string hellenicFieldType = hellenicField.Attribute(HellenicXMLDefinitions.TYPE_ATTRIBUTE).Value;
                        string hellenicCsFieldType = HellenicXMLDefinitions.TypeMap[hellenicFieldType];

                        if (HellenicXMLDefinitions.FunctionMap.ContainsKey(defaultValue))
                        {
                            // Get the function type and cast to the C# type for this field
                            defaultValue = $"({hellenicCsFieldType}){HellenicXMLDefinitions.FunctionMap[defaultValue]}";
                        }

                        stringBuilder.Append(
                            $"\t\t\tp{hellenicFieldNamePascal}: {defaultValue},\n");

                    }
                    else
                    {
                        // The conversion expression for converting the mavlink value to Hellenic value
                        string conversionExpr = correspondingMapping.Attribute(HellenicXMLDefinitions.CONVERSION_ATTRIBUTE).Value;
                        // The name of the mavlink message field that corresponds to this hellenic field
                        string mappedMavlinkMessageField = correspondingMapping.Attribute(HellenicXMLDefinitions.COMMON_FIELD_NAME_ATTRIBUTE).Value;

                        // Final parameter to be placed in our constructor arguments, e.g., "mavlinkMessage.sysid"
                        string mappedMavlinkMessageFieldParam =
                            $"{HellenicXMLDefinitions.MAVLINK_MESSAGE_PARAM}.{mappedMavlinkMessageField}";

                        string finalConversion = conversionExpr.Replace(HellenicXMLDefinitions.CONVERSION_VALUE_EXPRESSION_PARAMETER, mappedMavlinkMessageField);

                        stringBuilder.Append(
                            $"\t\t\tp{hellenicFieldNamePascal}: {finalConversion},\n");

                    }

                }

            }

            // var LatitudeLongitudeHellenicMessage = new LatitudeLongitude(
            //     pMachineId: mavlinkMessage.sysid,
            //     pOriginalProtocol: (uint)Protocols.Mavlink,
            //     pLat: mavlinkData.lat / 10000000.0,
            //     pLon: mavlinkData.lon / 10000000.0,
            //     pTimeUsec: mavlinkData.time_boot_ms,
            //     pReferenceFrame: 2
            // );

        }

        return stringBuilder;
    }


    private static StringBuilder GenerateTranslationDictionaryDefinition()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(HellenicXMLDefinitions.TRANSLATOR_DICT_DECLARATION);



        return stringBuilder;
    }

    private static StringBuilder GenerateTranslationFunction()
    {
        return new StringBuilder();
    }

    public static void Generate(string[] args)
    {

        if(args.Length > 4)
        {
            throw new ArgumentException(
                "There are only four arguments: " +
                "input XML Hellenic file, input MAVLink XML file (e.g., common.xml),  " +
                "input XML translation file (e.g., common_to_hellenic.xml), and output directory");
        }

        string hellenicXMLPath = args[0];
        string mavlinkXMLPath = args[1];
        string translationXMLPath = args[2];
        OUTPUT_DIRECTORY = args[3];

        XDocument hellenicXML;
        try
        {
            hellenicXML = XDocument.Load(hellenicXMLPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        XDocument translationXML;
        try
        {
            translationXML = XDocument.Load(translationXMLPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        // MAVLink's XML files have <include> statements that include other XMLs, so we just grab all those XMLs here
        HashSet<XDocument> allMAVLinkXMLs = GeneratorUtils.GetIncludedXMLs(mavlinkXMLPath);
        XElement mavlinkMessagesElement = null;

        // Merge the messages
        foreach (XDocument mavlinkXML in allMAVLinkXMLs)
        {
            mavlinkMessagesElement = GeneratorUtils.MergeElements(mavlinkMessagesElement, mavlinkXML.Root.Element(HellenicXMLDefinitions.MESSAGES_ELEMENT));
        }

        StringBuilder finalClass = new StringBuilder();

        finalClass.Append(GenerateTranslationClassHeader());
        finalClass.Append(GenerateTranslationAPI());
        finalClass.Append(GenerateTranslationFunctions(
            translationXML.Root.Element(HellenicXMLDefinitions.CONVERSIONS_ELEMENT),
            hellenicXML.Root.Element(HellenicXMLDefinitions.MESSAGES_ELEMENT),
            mavlinkMessagesElement
        ));
        finalClass.Append(GenerateTranslationDictionaryDefinition());

        Console.WriteLine(finalClass.ToString());

        // File.WriteAllText($"{OUTPUT_DIRECTORY}/{HellenicXMLDefinitions.TRANSLATOR_CLASS_NAME}.cs", finalClass.ToString());

    }
}
