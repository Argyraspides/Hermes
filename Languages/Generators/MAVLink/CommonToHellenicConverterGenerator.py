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
g_class_header = '''using System;
using System.Collections.Generic;
using System.IO;


/*
This translator converts MAVLink message objects directly to Hellenic message objects.
It works with the C# MAVLink parser in MAVLink.dll to process binary MAVLink data.

Example usage:
MAVLink.MAVLinkMessage mavlinkMsg = new MAVLink.MAVLinkMessage(rawBytes);
List<HellenicMessage> hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(mavlinkMsg);
*/
class MAVLinkToHellenicTranslator
{'''

g_translate_message_function = '''
    public static List<HellenicMessage> TranslateMAVLinkMessage(MAVLink.MAVLinkMessage mavlinkMessage)
    {
        // Extract the message ID
        uint msgId = mavlinkMessage.msgid;

        // Look up the appropriate conversion function
        if (MAVLinkIdToConversionFunctionDict.TryGetValue(msgId, out var conversionFunc))
        {
            return conversionFunc(mavlinkMessage);
        }

        // No suitable translation function found
        Console.WriteLine("Unable to translate MAVLink message! No suitable translation function found for msgid: " + msgId);
        return new List<HellenicMessage>();
    }
'''

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

# Dictionary to store MAVLink IDs to function name mappings
g_function_map = {}

# XML tag and attribute names
g_common_to_hellenic_tag_string_name = "common_to_hellenic"
g_conversions_tag_string_name = "conversions"
g_message_tag_string_name = "message"
g_mapping_tag_string_name = "mapping"
g_common_id_attribute_string_name = "common_id"
g_common_field_name_attribute_string_name = "common_field_name"
g_common_field_tag_string_name = "field"
g_hellenic_id_attribute_string_name = "hellenic_id"
g_hellenic_field_name_attribute_string_name = "hellenic_field_name"
g_hellenic_field_type_attribute_string_name = "hellenic_field_type"
g_hellenic_field_units_attribute_string_name = "hellenic_field_units"
g_hellenic_default_value_tag_string_name = "default_value"
g_conversion_attribute_string_name = "conversion"
g_messages_tag_string_name = "messages"
g_message_field_type_attr_string_name = "type"
g_message_field_name_attr_string_name = "name"
g_message_field_units_attr_string_name = "units"
g_message_id_attr_string_name = "id"
g_message_name_attr_string_name = "name"
g_message_description_tag_string_name = "description"
g_field_description_tag_string_name = "description"
g_message_fields_tag_string_name = "fields"
g_hellenic_interface_string_name = "HellenicMessage"


def snake_to_pascal_case(snake_case_string: str) -> str:
    """Convert snake_case to PascalCase."""
    return ''.join(word.capitalize() for word in snake_case_string.split("_"))


def generate_translation_functions(other_language_file_path, hellenic_language_file_path, translation_file_path):
    """
    Generate C# translation functions from MAVLink to Hellenic based on XML definitions.

    Args:
        other_language_file_path: Path to the MAVLink XML definition file
        hellenic_language_file_path: Path to the Hellenic XML definition file
        translation_file_path: Path to the translation mapping XML file

    Returns:
        str: C# code containing all the translation functions
    """
    mavlink_language_xml = ET.parse(other_language_file_path)
    hellenic_language_xml = ET.parse(hellenic_language_file_path)
    translation_xml = ET.parse(translation_file_path)

    hellenic_messages = hellenic_language_xml.getroot().find(g_messages_tag_string_name)
    mavlink_messages = mavlink_language_xml.getroot().find(g_messages_tag_string_name)
    translation_definitions = translation_xml.getroot().find(g_conversions_tag_string_name)

    all_functions = []

    for translation_definition in translation_definitions:
        mavlink_id = translation_definition.get(g_common_id_attribute_string_name)
        mavlink_message_definition = mavlink_messages.find(f"./{g_message_tag_string_name}[@id='{mavlink_id}']")

        mavlink_message_name = mavlink_message_definition.get(g_message_name_attr_string_name)
        function_name = f"{snake_to_pascal_case(mavlink_message_name)}ToHellenic"

        # Store the function name in the global map for later use
        g_function_map[mavlink_id] = function_name

        # Start building the function
        function_lines = [
            f"    public static List<HellenicMessage> {function_name}(MAVLink.MAVLinkMessage mavlinkMessage)",
            "    {",
            "        // Extract the MAVLink struct from the message object",
            f"        var mavlinkData = mavlinkMessage.ToStructure<MAVLink.mavlink_{mavlink_message_name.lower()}_t>();"
        ]

        # Collect all the Hellenic messages that this MAVLink message maps to
        hellenic_message_data = {}

        # First pass: identify all unique Hellenic messages
        for mapping in translation_definition:
            if mapping.tag in [g_mapping_tag_string_name, g_hellenic_default_value_tag_string_name]:
                hellenic_id = mapping.get(g_hellenic_id_attribute_string_name)

                if hellenic_id not in hellenic_message_data:
                    hellenic_message_def = hellenic_messages.find(f"./{g_message_tag_string_name}[@id='{hellenic_id}']")
                    hellenic_message_name = hellenic_message_def.get(g_message_name_attr_string_name)
                    pascal_name = snake_to_pascal_case(hellenic_message_name)

                    hellenic_message_data[hellenic_id] = {
                        "name": pascal_name,
                        "var_name": f"{pascal_name}HellenicMessage",
                        "fields": []
                    }

        # Now process all mappings and default values
        for mapping in translation_definition:
            hellenic_id = mapping.get(g_hellenic_id_attribute_string_name)

            if mapping.tag == g_hellenic_default_value_tag_string_name:
                # Handle default values
                hellenic_field_name = mapping.get(g_hellenic_field_name_attribute_string_name)
                value = mapping.get("value")

                hellenic_message_data[hellenic_id]["fields"].append({
                    "name": hellenic_field_name,
                    "value": value,
                    "is_default": True
                })

            elif mapping.tag == g_mapping_tag_string_name:
                # Handle field mappings
                mavlink_field_name = mapping.get(g_common_field_name_attribute_string_name)
                hellenic_field_name = mapping.get(g_hellenic_field_name_attribute_string_name)
                conversion = mapping.get(g_conversion_attribute_string_name)

                # Find the MAVLink field type
                mavlink_field = mavlink_message_definition.find(f"./field[@name='{mavlink_field_name}']")
                if mavlink_field is None:
                    mavlink_field = mavlink_message_definition.find(f".//field[@name='{mavlink_field_name}']")

                if mavlink_field is not None:
                    mavlink_field_type = mavlink_field.get(g_message_field_type_attr_string_name)
                    mavlink_c_sharp_field_type = g_type_map.get(mavlink_field_type, "object")
                else:
                    # Default to object if field type can't be determined
                    mavlink_c_sharp_field_type = "object"

                hellenic_message_data[hellenic_id]["fields"].append({
                    "name": hellenic_field_name,
                    "mavlink_field_name": mavlink_field_name,
                    "mavlink_field_type": mavlink_c_sharp_field_type,
                    "conversion": conversion,
                    "is_default": False
                })

        function_lines.append("")

        # Generate constructor calls for each Hellenic message
        for hellenic_id, message_info in sorted(hellenic_message_data.items()):
            constructor_lines = [f"        var {message_info['var_name']} = new {message_info['name']}("]

            field_params = []
            for field in message_info["fields"]:
                param_name = f"p{snake_to_pascal_case(field['name'])}"

                if field["is_default"]:
                    value = field["value"]
                else:
                    # Value from MAVLink with conversion
                    value_expr = f"mavlinkData.{field['mavlink_field_name']}"
                    if field["conversion"] == "value":
                        value = value_expr
                    else:
                        value = field["conversion"].replace("value", value_expr)

                field_params.append(f"            {param_name}: {value}")

            constructor_lines.append(",\n".join(field_params))
            constructor_lines.append("        );")

            function_lines.extend(constructor_lines)
            function_lines.append("")

        # Add return statement with all Hellenic messages
        function_lines.append("        return new List<HellenicMessage> {")
        for _, message_info in sorted(hellenic_message_data.items()):
            function_lines.append(f"            {message_info['var_name']},")

        # Remove trailing comma from the last line
        if function_lines[-1].endswith(","):
            function_lines[-1] = function_lines[-1][:-1]

        function_lines.append("        };")
        function_lines.append("    }")

        all_functions.append("\n".join(function_lines))

    return "\n\n".join(all_functions)


def generate_function_dictionary():
    """
    Generate C# code for the dictionary mapping MAVLink message IDs to conversion functions.

    Returns:
        str: C# code for the function dictionary
    """
    # Map to hold the message IDs to function dictionary
    lines = [
        "    public static Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>> MAVLinkIdToConversionFunctionDict",
        "    =",
        "    new Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>>()",
        "    {"
    ]

    # Add each mapping
    for id, function_name in sorted(g_function_map.items()):
        lines.append(f"        {{ {id}, {function_name} }},")

    # Remove trailing comma from the last line
    if lines[-1].endswith(","):
        lines[-1] = lines[-1][:-1]

    lines.append("    };")

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Generate C# message dialect translator from XML files")

    parser.add_argument("--input_translation_XML", required=True, help="File path of the translation XML")
    parser.add_argument("--input_original_XML", required=True,
                        help="File path of the original dialect XML to generate translation functions to Hellenic for")
    parser.add_argument("--input_hellenic_XML", required=True, help="File path of the Hellenic dialect XML definitions")
    parser.add_argument("--output_dir", required=True, help="Output directory for the generated C# translator module")

    '''
    E.g.,
    python3 CommonToHellenicConverterGenerator.py --input_translation_XML ../../DialectConversionDefinitions/common_to_hellenic.xml --input_original_XML ../../DialectDefinitions/common.xml --input_hellenic_XML ../../DialectDefinitions/hellenic.xml --output_dir ../../ConcreteConversions/ToHellenic
    '''

    args = parser.parse_args()

    functions_string = generate_translation_functions(
        args.input_original_XML,
        args.input_hellenic_XML,
        args.input_translation_XML
    )

    function_dictionary_string = generate_function_dictionary()

    final_file = g_class_header + "\n" + g_translate_message_function + "\n" + functions_string + "\n\n" + function_dictionary_string + "\n}"

    output_file_path = os.path.join(args.output_dir, "MAVLinkToHellenicTranslator.cs")
    os.makedirs(args.output_dir, exist_ok=True)

    with open(output_file_path, "w") as f:
        f.write(final_file)

    print(f"Successfully generated C# translator at: {output_file_path}")


if __name__ == "__main__":
    main()
