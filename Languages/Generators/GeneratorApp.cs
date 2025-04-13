namespace Hermes.Languages.Generators;


// ***************************************************************
//
// To run:
//
// ***************************************************************
public class GeneratorApp
{

    private static string GEN_HELLENIC_MESSAGES_FLAG = "--generateHellenicMessages";
    private static string GEN_HELLENIC_TRANSLATOR_FLAG = "--generateMAVLinkToHellenicTranslator";

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            PrintOptions();
            return;
        }

        if (args[0] == GEN_HELLENIC_MESSAGES_FLAG)
        {
            args = args.Skip(1).ToArray();
            HellenicMessageGenerator.Generate(args);
        }
        else if (args[0] == GEN_HELLENIC_TRANSLATOR_FLAG)
        {
            args = args.Skip(1).ToArray();
            MAVLinkToHellenicGenerator.Generate(args);
        }
    }


    private static void PrintOptions()
    {

        Console.ForegroundColor = ConsoleColor.Red;

        Console.Write("Usage for GeneratorApp:\n");
        Console.Write("Options:\n\n");


        Console.Write("TO GENERATE HELLENIC MESSAGES:\n\n");
        Console.Write(
            $"\t\t dotnet run {GEN_HELLENIC_MESSAGES_FLAG} " +
            $"../ProtocolXMLDefinitions/hellenic.xml " +
            $"../ProtocolCSharpDefinitions/Hellenic");


        Console.WriteLine(new string('*', 75));


        Console.Write("TO GENERATE MAVLINK TO HELLENIC TRANSLATOR:\n\n");
        Console.Write(
            $"\t\t dotnet run {GEN_HELLENIC_TRANSLATOR_FLAG} " +
            $"../ProtocolXMLDefinitions/hellenic.xml " +
            $"../ProtocolXMLDefinitions/common.xml" +
            $"../ProtocolXMLConversionDefinitions/common_to_hellenic.xml" +
            $"../ProtocolConverters/ToHellenic"
            );

        Console.ResetColor();
    }

}
