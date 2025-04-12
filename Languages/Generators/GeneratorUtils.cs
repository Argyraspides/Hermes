using System.Text;
using System.Text.RegularExpressions;

public static class GeneratorUtils
{

    public const uint MAX_WORDS_PER_LINE = 10;

    public static string SnakeToPascal(string str)
    {
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
