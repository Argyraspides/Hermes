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


import argparse
import os
import xml.etree.ElementTree as ET

# This script takes in an XML file that defines the names, attributes, and fields of all Hellenic messages,
# and generates C# definitions for them

# Check the __main__ to see arguments

# Quick run: python3 HellenicDialectGenerator.py  --input_XML ../DialectDefinitions/hellenic.xml --output_dir ../ConcreteDialects/Hellenic/

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


def generate_hellenic_interface_file(output_dir: str) -> None:
    interface_content = (f""
                         f"using Godot;\n\n\n"
                         f"public abstract partial class HellenicMessage : RefCounted\n"
                         f"{{\n"
                         f"\t// The ID of the machine this message was sent from\n"
                         f"\tpublic uint MachineId {{ get; protected set; }} = uint.MaxValue;\n"
                         f"\t// The ID of the Hellenic message itself. E.g., An ID of 0 corresponds to \"LatitudeLongitude\"\n"
                         f"\tpublic uint Id {{ get; protected set; }} = uint.MaxValue;\n"
                         f"\t// The name of this Hellenic message, e.g., \"LatitudeLongitude\"\n"
                         f"\tpublic string MessageName {{ get; protected set; }} = string.Empty;\n"
                         f"}}")

    output_path = os.path.join(output_dir, "HellenicMessage.cs")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        f.write(interface_content)


def generate_dialect_class(hellenic_xml_message, hellenic_xml_message_header) -> str:
    # GENERATE BODY OF THE CLASS >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    hellenic_description = hellenic_xml_message.find("description").text
    hellenic_description_comment = generate_description_comment(hellenic_description)
    hellenic_class_name = hellenic_xml_message.get("name")
    hellenic_class_header = f"partial class {snake_to_pascal_case(hellenic_class_name)} : HellenicMessage\n{{\n"

    final_class = hellenic_description_comment + hellenic_class_header

    hellenic_fields = hellenic_xml_message.find("fields")

    for field in hellenic_fields:
        field_description = field.find("description").text
        field_description_comment = generate_description_comment(field_description)
        field_c_sharp_type = g_type_map.get(field.get("type"))
        field_name = field.get("name")
        field_name_pascal_case = snake_to_pascal_case(field_name)
        field_line = f"\tpublic {field_c_sharp_type} {field_name_pascal_case} {{ get; set; }}\n\n"
        final_class += field_description_comment + field_line

    # GENERATE EMPTY CONSTRUCTOR >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    hellenic_message_id = hellenic_xml_message.get("id")
    hellenic_class_name_pascal_case = snake_to_pascal_case(hellenic_class_name)
    # TODO::ARGYRASPIDES() { See how "Id" is hardcoded? Field names like this should be parametrized somehow }
    empty_constructor = (f""
                         f"\tpublic {hellenic_class_name_pascal_case}()\n"
                         f"\t{{\n"
                         f"\t\tId = {hellenic_message_id};\n"
                         f"\t\tMessageName = nameof({hellenic_class_name_pascal_case});\n"
                         f"\t}}\n\n"
                         )

    final_class += empty_constructor

    # GENERATE FULL CONSTRUCTOR >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    # Constructor parameters inside the actual message
    constructor_params = ""
    constructor_body = ""
    for field in hellenic_fields:
        field_name = field.get("name")
        field_name_pascal_case = snake_to_pascal_case(field_name)
        field_c_sharp_type = g_type_map.get(field.get("type"))

        # Prepending parameters with 'p' to avoid confusion in the body
        constructor_params += f"{field_c_sharp_type} p{field_name_pascal_case}, "
        constructor_body += f"\t\t{field_name_pascal_case} = p{field_name_pascal_case};\n"

    # Constructor parameters for the abstract hellenic message
    hellenic_header_fields = hellenic_xml_message_header.find("fields")
    for field in hellenic_header_fields:
        hellenic_header_field_name = field.get("name")
        hellenic_header_field_type = field.get("type")

        hellenic_header_field_c_sharp_type = g_type_map.get(hellenic_header_field_type)
        hellenic_header_field_name_pascal_case = snake_to_pascal_case(hellenic_header_field_name)

        constructor_params += f"{hellenic_header_field_c_sharp_type} p{hellenic_header_field_name_pascal_case}, "
        constructor_body += f"\t\t{hellenic_header_field_name_pascal_case} = p{hellenic_header_field_name_pascal_case};\n"

    # Remove the trailing ","
    constructor_params = constructor_params[:-2]

    full_constructor = (f""
                        f"\tpublic {hellenic_class_name_pascal_case}({constructor_params})\n"
                        f"\t{{\n"
                        f"\t\tId = {hellenic_message_id};\n"
                        f"\t\tMessageName = nameof({hellenic_class_name_pascal_case});\n"
                        f"{constructor_body}"
                        f"\t}}\n\n"
                        )

    final_class += full_constructor
    final_class += "}"
    return final_class


def generate_dialect_classes(hellenic_xml_file_path: str, output_dir_path: str) -> None:
    hellenic_xml_root = ET.parse(hellenic_xml_file_path)

    hellenic_xml_message_enums = hellenic_xml_root.find("enums")
    hellenic_xml_message_header = hellenic_xml_root.find("header")
    hellenic_xml_messages = hellenic_xml_root.find("messages")

    for message in hellenic_xml_messages:
        class_file = generate_dialect_class(message, hellenic_xml_message_header)
        message_name = message.get("name")
        message_name_pascal_case = snake_to_pascal_case(message_name)
        output_path = os.path.join(output_dir_path, f"{message_name_pascal_case}.cs")
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, "w") as f:
            f.write(class_file)


def generate_description_comment(description: str) -> str:
    opening_tag = "\t/// <summary>\n"

    inner_description_comment = ""

    description = description.strip()
    description_words_by_newline = description.split("\n")

    for line in description_words_by_newline:
        line = line.strip()
        if not line: continue
        inner_description_comment += f"\t/// {line}\n"

    closing_tag = "\t/// </summary>\n"

    return opening_tag + inner_description_comment + closing_tag


def snake_to_pascal_case(snake_case_string: str) -> str:
    return ''.join(word.capitalize() for word in snake_case_string.split("_"))


# Generates enum file for the messages corresponding to their IDs
def generate_enum_file(hellenic_xml_file_path: str, output_dir_path: str) -> None:
    hellenic_xml_root = ET.parse(hellenic_xml_file_path)

    hellenic_xml_messages = hellenic_xml_root.find("messages")

    enum_header = (f""
                   f"public enum HellenicMessageType\n"
                   f"{{\n"
                   )
    enum_body = ""
    for message in hellenic_xml_messages:
        message_id = message.get("id")
        message_name = message.get("name")
        message_name_pascal_case = snake_to_pascal_case(message_name)

        enum_body += f"\t{message_name_pascal_case} = {message_id},\n"

    # Remove trailing comma but keep the \n
    enum_body = enum_body[:-2] + enum_body[-1]

    final_enum_file = enum_header + enum_body + "}"

    output_file_path = os.path.join(output_dir_path, "HellenicMessageType.cs")
    os.makedirs(os.path.dirname(output_file_path), exist_ok=True)
    with open(output_file_path, "w") as f:
        f.write(final_enum_file)

# Generates enum files for each and every single enum definition in the hellenic message set
def generate_enum_definition_files(hellenic_xml_file_path: str, output_dir_path: str) -> None:

    hellenic_xml_root = ET.parse(hellenic_xml_file_path)
    hellenic_enums = hellenic_xml_root.find("enums")

    for enum in hellenic_enums:
        enum_name = enum.get("name")
        enum_name_pascal_case = snake_to_pascal_case(enum_name)
        enum_description = enum.find("description").text
        enum_description_comment = generate_description_comment(enum_description)

        enum_header = (f""
                       f"{enum_description_comment}"
                       f"public enum {enum_name_pascal_case}\n"
                       f"{{\n"
                       )

        enum_body = ""
        enum_entries = enum.find("entries")
        for entry in enum_entries:
            entry_name = entry.get("name")
            entry_name_pascal_case = snake_to_pascal_case(entry_name)
            enum_value = entry.get("value")
            entry_description = entry.find("description").text
            entry_description_comment = generate_description_comment(entry_description)

            enum_body += entry_description_comment
            enum_body += f"\t{entry_name_pascal_case} = {enum_value},\n"

        # Remove trailing comma
        enum_body = enum_body[:-2] + "\n"

        enum_file = enum_header + enum_body + "}"

        output_path = os.path.join(output_dir_path, "Enums", f"{enum_name_pascal_case}.cs")
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, "w") as f:
            f.write(enum_file)



if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate C# message dialect from an XML file")

    parser.add_argument("--input_XML", required=True, help="File path to XML to turn into C# messages")
    parser.add_argument("--output_dir", required=True, help="Output directory for the generated C# messages")

    args = parser.parse_args()

    generate_hellenic_interface_file(args.output_dir)
    generate_dialect_classes(args.input_XML, args.output_dir)
    generate_enum_file(args.input_XML, args.output_dir)
    generate_enum_definition_files(args.input_XML, args.output_dir)
