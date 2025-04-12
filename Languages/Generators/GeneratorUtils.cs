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


using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public static class GeneratorUtils
{

    public const uint MAX_WORDS_PER_LINE = 10;
    public const uint MAX_XML_INCLUDES = 500;

    public static string SnakeToPascal(string str)
    {

        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = new StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in str)
        {
            if(c == '_')
            {
                capitalizeNext = true;
            }
            else
            {
                stringBuilder.Append(capitalizeNext ? char.ToUpper(c) : char.ToLower(c));
                capitalizeNext = false;
            }
        }

        return stringBuilder.ToString();
    }

    public static string DescriptionToSummaryComment(string str)
    {

        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("/// <summary>\n/// ");

        // https://regexone.com/ is a goated site for this shit
        str = Regex.Replace(str, @"(\n+)|(\s+)", " ");
        string[] words = Regex.Split(str, @"\s");

        uint currentWordsPerLine = 0;
        for(int i = 0; i < words.Length; i++)
        {
            if(currentWordsPerLine++ > MAX_WORDS_PER_LINE)
            {
                currentWordsPerLine = 0;
                stringBuilder.Append("\n/// ");
            }

            stringBuilder.Append($"{words[i]} ");
        }


        stringBuilder.Append("\n/// </summary>");
        return stringBuilder.ToString();

    }

    // Merges the contents of two XML elements together
    public static XElement MergeElements(XElement element1, XElement element2)
    {

        if (element1 == null || element2 == null)
        {
            throw new ArgumentNullException();
        }

        if (element1.Name != element2.Name)
        {
            throw new ArgumentException("Elements must have the same name in order to merge them!");
        }

        XElement result = new XElement(element1.Name);


        // TODO::ARGYRASPIDES() { FINISH THIS }


        return result;

    }

    // Recursively fetches all XMLs in an <include> element. The value
    // inside the element is treated as a relative path from the location of the XML file.
    // The include must be inside the root element
    public static HashSet<XDocument> GetIncludedXMLs(string path)
    {

        HashSet<XDocument> includedXMLs = new HashSet<XDocument>();

        XDocument mainXml = XDocument.Load(path);
        XElement rootElement = mainXml.Root ?? throw new NoNullAllowedException("Could not load main XML, no root element!");

        for(int i = 0; i < MAX_XML_INCLUDES; i++)
        {
            var includeElement = rootElement.Element("include");
            if(includeElement == null) break;

            string nextXmlPath = includeElement.Value;

            XDocument nextXml = XDocument.Load($"{Directory.GetCurrentDirectory()}/{nextXmlPath}");

            if(includedXMLs.Contains(nextXml))
            {
                throw new InvalidOperationException("Detected circular includes!");
            }

            includedXMLs.Add(nextXml);
            rootElement = nextXml.Root ?? throw new NoNullAllowedException();

        }


        return includedXMLs;

    }

}
