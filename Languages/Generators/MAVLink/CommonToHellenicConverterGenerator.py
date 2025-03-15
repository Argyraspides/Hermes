#
#
# 88        88  88888888888  88888888ba   88        88         db         88888888888  ad88888ba  888888888888  88        88   ad88888ba
# 88        88  88           88      "8b  88        88        d88b        88          d8"     "8b      88       88        88  d8"     "8b
# 88        88  88           88      ,8P  88        88       d8'`8b       88          Y8,              88       88        88  Y8,
# 88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88aaaaaaaa88      d8'  `8b      88aaaaa     `Y8aaaaa,        88       88        88  `Y8aaaaa,
# 88""""""""88  88"""""      88""""""'    88""""""""88     d8YaaaaY8b     88"""""       `"""""8b,      88       88        88    `"""""8b,
# 88        88  88           88           88        88    d8""""""""8b    88                  `8b      88       88        88          `8b
# 88        88  88           88           88        88   d8'        `8b   88          Y8a     a8P      88       Y8a.    .a8P  Y8a     a8P
# 88        88  88888888888  88           88        88  d8'          `8b  88888888888  "Y88888P"       88        `"Y8888Y"'    "Y88888P"
#
#

# Note: This script translates from MAVLink to Hellenic dialect using auto-generated C# MAVLink message structs

import argparse
import os
import xml.etree.ElementTree as ET

# This script takes in an XML file that defines mappings from MAVLink messages to Hellenic messages

# Constants for C# code generation
g_class_header = (f""
                  f"using System;\n"
                  f"using System.Collections.Generic;\n"
                  f"using System.IO;\n"
                  f"\n"
                  f"\n"
                  f"/*\n"
                  f"This translator converts MAVLink message objects directly to Hellenic message objects.\n"
                  f"It works with the C# MAVLink parser in MAVLink.dll to process binary MAVLink data.\n"
                  f"\n"
                  f"Example usage:\n"
                  f"MAVLink.MAVLinkMessage mavlinkMsg = new MAVLink.MAVLinkMessage(rawBytes);\n"
                  f"List<HellenicMessage> hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(mavlinkMsg);\n"
                  f"*/\n"
                  f"class MAVLinkToHellenicTranslator\n"
                  f"{{\n")

g_translate_message_function = (f""
                                f"\tpublic static List<HellenicMessage> TranslateMAVLinkMessage(MAVLink.MAVLinkMessage mavlinkMessage)\n"
                                f"\t{{\n"
                                f"\t\t// Extract the message ID\n"
                                f"\t\tuint msgId = mavlinkMessage.msgid;\n"
                                f"\t\t// Look up the appropriate conversion function\n"
                                f"\t\tif (MAVLinkIdToConversionFunctionDict.TryGetValue(msgId, out var conversionFunc))\n"
                                f"\t\t{{\n"
                                f"\t\t\treturn conversionFunc(mavlinkMessage);\n"
                                f"\t\t}}\n"
                                f"\t\t// No suitable translation function found\n"
                                f"\t\tConsole.WriteLine(\"Unable to translate MAVLink message! No suitable translation function found for msgid: \" + msgId);\n"
                                f"\t\treturn new List<HellenicMessage>();\n"
                                f"\t}}\n\n")

# Map from the XML type definitions to a C# equivalent
g_type_map = {
    "uint8_t": "byte",
    "uint16_t": "ushort",
    "uint32_t": "uint",
    "uint64_t": "ulong",
    "int8_t": "sbyte",
    "int16_t": "short",
    "int32_t": "int",
    "int64_t": "long",
    "float": "float",
    "float32": "float",
    "float64": "double",
    "char": "char",
    "string": "string"
}

# Dictionary that contains the names of the translation functions mapped to the MAVLink ID
# that they correspond to
g_translation_function_dict = {}


def generate_function(common_xml_message, hellenic_xml_root, translation_xml):
    common_message_name = common_xml_message.get("name")
    common_message_name_pascal_case = snake_to_pascal_case(common_message_name)

    common_struct_name = f"mavlink_{common_message_name.lower()}_t"

    g_translation_function_dict[common_xml_message.get("id")] = f"{common_message_name_pascal_case}ToHellenic"

    function_header = (f""
                       f"\tpublic static List<HellenicMessage> {common_message_name_pascal_case}ToHellenic(MAVLink.MAVLinkMessage mavlinkMessage)\n"
                       f"\t{{\n"
                       f"\t\t// Extract the MAVLink struct from the message object\n"
                       f"\t\tvar mavlinkData = mavlinkMessage.ToStructure<MAVLink.{common_struct_name}>();\n"
                       f"\n")

    # The msg_param_dict should look as follows (if you need to add extra parameters, simply define them in the xml file broskee):
    '''
    msg_param_dict =
    {
        "LatitudeLongitude":
        {
            "lat": "mavlinkData.lat / 10000000.0",
            "lon": "mavlinkData.lon / 10000000.0",
            "time_usec: "mavlinkData.time_boot_ms",
            "reference_frame": 2,                    # DEFAULT VALUE!
            "vehicle_id": mavlinkMessage.sysid
        },
        "Altitude":
        {
          ...
        }
    }
    '''
    # Go through the XML files and make the dictionary above
    msg_param_dict = {}

    for mapping in translation_xml:

        hellenic_id = mapping.get("hellenic_id")
        hellenic_xml_message = hellenic_xml_root.find(f'.//message[@id="{hellenic_id}"]')

        # E.g., "LATITUDE_LONGITUDE"
        hellenic_message_name = hellenic_xml_message.get('name')
        # And to "LatitudeLongitude"
        hellenic_message_name_pascal_case = snake_to_pascal_case(hellenic_message_name)

        if hellenic_message_name_pascal_case not in msg_param_dict:
            # All messages must include a vehicle/system id
            # TODO::ARGYRASPIDES() { Again with this derived message stuff... we shouldnt have to do this bruh }
            # should all be defined in the XML with zero hardcoding in the generator script
            msg_param_dict[hellenic_message_name_pascal_case] = {"entity_id": "mavlinkMessage.sysid"}

        if mapping.tag == "default_value":
            hellenic_message_field_name = mapping.get("hellenic_field_name")
            hellenic_field_default_value = mapping.get("value")
            msg_param_dict[hellenic_message_name_pascal_case][
                hellenic_message_field_name] = hellenic_field_default_value
            continue

        # Field name of the common message we are about to map to a hellenic message
        common_message_field_name = mapping.get("common_field_name")
        # Field name of the hellenic message we are mapping to
        hellenic_message_field_name = mapping.get("hellenic_field_name")

        value = f"mavlinkData.{common_message_field_name}"

        # Will give us a string back like "value / 10000000.0"
        conversion = mapping.get('conversion')
        # Replace "value" string to get converted value, e.g., "mavlinkData.lat / 10000000.0"
        conversion = conversion.replace("value", f"{value}")

        msg_param_dict[hellenic_message_name_pascal_case][hellenic_message_field_name] = conversion

    # Use the dictionary to make a clean thingy declaration thingy mabober (bober means beaver in Polish btw)
    for hellenic_message, hellenic_message_params in msg_param_dict.items():
        conversion_declaration = (f""
                                  f"\t\tvar {hellenic_message}HellenicMessage = new {hellenic_message}("
                                  f"\n")

        for hellenic_message_param_name, hellenic_message_param_value in hellenic_message_params.items():
            param_name_pascal_case = snake_to_pascal_case(hellenic_message_param_name)
            conversion_declaration += f"\t\t\tp{param_name_pascal_case}: {hellenic_message_param_value},\n"

        # Remove the trailing comma here but keep that \n
        conversion_declaration = conversion_declaration[:-2] + conversion_declaration[-1]
        conversion_declaration += "\t\t);\n\n"
        function_header += conversion_declaration

    # Create the final return statement
    return_line = "\t\treturn new List<HellenicMessage>\n" + \
                  "\t\t{\n"
    for hellenic_message, _ in msg_param_dict.items():
        return_line += f"\t\t\t{hellenic_message}HellenicMessage,\n"

    # Again remove the traling comma but keep that \n
    return_line = return_line[:-2] + return_line[-1]
    return_line += "\t\t};\n"

    function_header += return_line
    function_header += "\t}\n\n"

    return function_header


def generate_dictionary():
    translation_dict_declaration = (f""
                                    f"\tpublic static Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>>\n"
                                    f"\t\tMAVLinkIdToConversionFunctionDict\n"
                                    f"\t\t\t=\n"
                                    f"\t\t\tnew Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>>()\n"
                                    f"\t\t\t{{\n")

    for common_id, function_name in g_translation_function_dict.items():
        translation_dict_declaration += "\t\t\t\t{" + f"{common_id}, {function_name}" + "},\n"

    # Remove the trailing gomma while keeping the \n brudda
    translation_dict_declaration = translation_dict_declaration[:-2] + translation_dict_declaration[-1]
    # FINAL BRACE LETS GOOOOO
    translation_dict_declaration += "\t\t\t};\n"

    return translation_dict_declaration


def snake_to_pascal_case(snake_case_string: str) -> str:
    return ''.join(word.capitalize() for word in snake_case_string.split("_"))


def generate_converter_file(common_XML_file_path, hellenic_XML_file_path, translation_XML_file_path):
    common_xml_root = ET.parse(common_XML_file_path).getroot()
    hellenic_xml_root = ET.parse(hellenic_XML_file_path).getroot()
    translation_xml_root = ET.parse(translation_XML_file_path).getroot()

    functions = ""
    final_file = g_class_header + g_translate_message_function

    # Go through all message translation definitions
    for common_message_mapping in translation_xml_root.find('conversions'):
        # Grab the MAVLink XML message definition for this translation
        common_message_id = common_message_mapping.get('common_id')
        common_message_xml = common_xml_root.find('messages').find(f'.//message[@id="{common_message_id}"]')

        conversion_function = generate_function(common_message_xml, hellenic_xml_root, common_message_mapping)
        functions += conversion_function

    final_file += functions
    final_file += generate_dictionary()
    final_file += "}"

    return final_file


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate C# message dialect from an XML file")

    parser.add_argument("--translation_XML", required=True, help="File path to XML to turn into C# messages")
    parser.add_argument("--common_XML", required=True, help="Output directory for the generated C# messages")
    parser.add_argument("--hellenic_XML", required=True, help="Hellenic message XML")
    parser.add_argument("--output_dir", required=True, help="Output directory for the translator")

    args = parser.parse_args()

    final_file = generate_converter_file(args.common_XML, args.hellenic_XML, args.translation_XML)
    output_path = os.path.join(args.output_dir, "MAVLinkToHellenicTranslator.cs")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        f.write(final_file)

# herpes
