using System;
using System.Collections;
using System.Collections.Generic;

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

    private static StringBuilder GenerateTelemetryTranslationFunctions(
        XElement conversionsElement,
        XElement hellenicMessagesElement,
        XElement mavlinkMessagesElement
        )
    {
        return new StringBuilder();
    }

    private static StringBuilder GenerateTranslationDictionaryDefinition()
    {
        return new StringBuilder(HellenicXMLDefinitions.TRANSLATOR_DICT_DECLARATION);
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
        finalClass.Append(GenerateTelemetryTranslationFunctions(
            translationXML.Root.Element(HellenicXMLDefinitions.CONVERSIONS_ELEMENT),
            hellenicXML.Root.Element(HellenicXMLDefinitions.MESSAGES_ELEMENT),
            mavlinkMessagesElement
        ));
        finalClass.Append(GenerateTranslationDictionaryDefinition());

    }
}
