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


using System.Text;
using System.Text.RegularExpressions;

public static class GeneratorUtils
{

    public const uint MAX_WORDS_PER_LINE = 10;

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
}
