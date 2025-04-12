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
    public const char SNAKE_CASE_DELIMITER = '_';

    public const string SUMMARY_COMMENT_START = "/// <summary>\n/// ";
    public const string SUMMARY_COMMENT_END = "\n/// </summary>";
    public const string SUMMARY_COMMENT_NEWLINE = "\n/// ";

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
            if(c == SNAKE_CASE_DELIMITER)
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
        stringBuilder.Append(SUMMARY_COMMENT_START);

        // https://regexone.com/ is a goated site for this shit
        str = Regex.Replace(str, @"(\n+)|(\s+)", " ");
        string[] words = Regex.Split(str, @"\s");

        uint currentWordsPerLine = 0;
        for(int i = 0; i < words.Length; i++)
        {
            if(currentWordsPerLine++ > MAX_WORDS_PER_LINE)
            {
                currentWordsPerLine = 0;
                stringBuilder.Append(SUMMARY_COMMENT_NEWLINE);
            }

            stringBuilder.Append($"{words[i]} ");
        }


        stringBuilder.Append(SUMMARY_COMMENT_END);
        return stringBuilder.ToString();

    }

    // Merges the contents of two XML elements together, provided they have the same name.
    // Contents are simply added together
    public static XElement MergeElements(XElement element1, XElement element2)
    {

        if(element1 != null && element2 == null)
        {
            return element1;
        }
        if(element1 == null && element2 != null)
        {
            return element2;
        }
        if(element1 == null && element2 == null)
        {
            throw new ArgumentNullException("Attempting to merge two null XElements! Returning ...");
        }
        if (element1.Name != element2.Name)
        {
            throw new ArgumentException("Elements must have the same name in order to merge them!");
        }

        IEnumerable<XElement> innerElements = element1.Descendants().Concat(element2.Descendants());
        XElement outerElement = new XElement(element1.Name);

        outerElement.Add(innerElements);

        return outerElement;

    }

    // For any XML document which contains an <include> in the root, recursively
    // searches all includes for other XMLs (similar to C++'s #include preprocessor directive),
    // and returns a set of all XML documents that are included
    public static HashSet<XDocument> GetIncludedXMLs(string path)
    {
        HashSet<XDocument> includedXMLs = new HashSet<XDocument>();
        HashSet<string> includedXMLPaths = new HashSet<string>();
        Queue<string> nextXmlPaths = new Queue<string>();

        string fullPath = Path.GetFullPath(path);
        nextXmlPaths.Enqueue(fullPath);

        while(nextXmlPaths.Count > 0)
        {
            string nextXmlPath = nextXmlPaths.Dequeue();

            if(includedXMLPaths.Contains(nextXmlPath))
            {
                throw new InvalidOperationException($"Circular includes detected at path: {nextXmlPath}");
            }

            XDocument nextXml;
            try
            {
                nextXml = XDocument.Load(nextXmlPath);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to load XML at {nextXmlPath}: {ex.Message}", ex);
            }

            includedXMLs.Add(nextXml);
            includedXMLPaths.Add(nextXmlPath);

            var includeElements = nextXml.Root?.Elements("include");
            if(includeElements == null)
            {
                continue;
            }

            string baseDir = Path.GetDirectoryName(nextXmlPath);
            foreach (XElement includeElement in includeElements)
            {
                string relativePath = includeElement.Value;
                string absolutePath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
                nextXmlPaths.Enqueue(absolutePath);
            }
        }

        return includedXMLs;
    }

}
