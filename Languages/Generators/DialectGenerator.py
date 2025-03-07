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

# The output class definitions will look like the following:

'''C#
    public class LatitudeLongitude : IHellenicMessage
    {

        public uint ID => 0;
        public string MessageName => nameof(LatitudeLongitude);

        public double Lat { get; set; }

        public double Lon { get; set; }

        public ulong TimeUsec { get; set; }

        public byte ReferenceFrame { get; set; }

        public LatitudeLongitude() { }
    }
'''

# Where IHellenicMessage is:

'''C#

    public interface IHellenicMessage
    {

        public uint ID { get; }
        public string MessageName { get; }

    }

'''

# The script assumes the XML is formatted as follows:

'''
<hellenic>
    <version>2</version>
    <dialect>1</dialect>
    <messages>
        <message id="2" name="GROUND_VELOCITY">
            <description>
                The velocity components of the object
            </description>
            <fields>
                <field type="float64" name="vx" units="m/s">
                    <description>Velocity X (Latitude direction, positive north)</description>
                </field>
        </message>
    </messages>
</hellenic>
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

# The actual string name of the <messages> tag
g_messages_tag_string_name = "messages"

# The actual string name of the attributes inside each <field> tag of a <message> tag
g_message_field_type_attr_string_name = "type"  # Data type of the field
g_message_field_name_attr_string_name = "name"  # Name of the field
g_message_field_units_attr_string_name = "units"  # Units that the field is in

# The actual string name of the attributes inside a <message> tag. All messages will have this "interface"
g_message_id_attr_string_name = "id"  # ID of the message
g_message_name_attr_string_name = "name"  # Name of the message

# The actual string name of the <description> tag inside each <message> tag
g_message_description_tag_string_name = "description"

# THe actual string name of the <description> tag inside each <field> tag
g_field_description_tag_string_name = "description"

# The actual string name of the <fields> tag inside each message tag
g_message_fields_tag_string_name = "fields"
# The actual string name of the interface that all of our messages will implement
g_hellenic_interface_string_name = "IHellenicMessage"


def snake_to_pascal_case(snake_case_string: str) -> str:
    return ''.join(word.capitalize() for word in snake_case_string.split("_"))


def generate_description_comment(description: str):
    opening_tag = "/// <summary>\n"

    inner_description_comment = ""

    description = description.strip()
    description_words_by_newline = description.split("\n")

    for line in description_words_by_newline:
        line = line.strip()
        if not line: continue
        inner_description_comment += f"/// {line}\n"

    closing_tag = "/// </summary>"

    return opening_tag + inner_description_comment + closing_tag


def generate_dialect_class_heading(class_name, class_description):
    header = f'''{class_description}\nclass {snake_to_pascal_case(class_name)} : {g_hellenic_interface_string_name}\n''' + "{\n"
    return header


def generate_hellenic_interface_file(output_dir):
    interface_content = f"""
public interface {g_hellenic_interface_string_name}
{{
    uint ID {{ get; }}
    string MessageName {{ get; }}
}}
""".strip()

    output_path = os.path.join(output_dir, f"{g_hellenic_interface_string_name}.cs")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        f.write(interface_content)


def generate_dialect_class(class_info_dict):
    class_description = generate_description_comment(class_info_dict[g_message_description_tag_string_name])
    class_heading = generate_dialect_class_heading(class_info_dict[g_message_name_attr_string_name], class_description)

    class_id = class_info_dict[g_message_id_attr_string_name]

    class_hellenic_interface_fields = f'''\tpublic uint ID => {class_id};
    public string MessageName => nameof({snake_to_pascal_case(class_info_dict[g_message_name_attr_string_name])});\n
'''

    # Dictionary for message will look like the following.
    # Contains all necessary information to generate a class definition
    # {
    #     "id": 0,
    #     "name": "LATITUDE_LONGITUDE",
    #     "description": "blah"
    #     "fields": {
    #
    #         "lat": {
    #             "type" : "float64"
    #             "description": "blah"
    #         },
    #         "lon": {
    #             "type" : "float64"
    #             "description": "blah"
    #         }
    #     }
    # }
    fields = class_info_dict["fields"]
    class_unique_fields = ""
    class_constructor_params = ""
    class_constructor_body = ""
    for field_name in fields:
        field_type = fields[field_name][g_message_field_type_attr_string_name]
        field_c_sharp_type = g_type_map[field_type]
        field_description = fields[field_name][g_field_description_tag_string_name]

        field_description_comment = generate_description_comment(field_description)

        # Add a tab to each line of the comments to align them with the member fields of the class
        formatted_comment = "".join(f"\t{line.strip()}\n" for line in field_description_comment.splitlines())

        member_field_line = f"{formatted_comment}\tpublic {field_c_sharp_type} {snake_to_pascal_case(field_name)} {{ get; set; }}\n\n"
        class_constructor_params += f"{field_c_sharp_type} p{snake_to_pascal_case(field_name)}, "
        class_constructor_body += f"\t\t{snake_to_pascal_case(field_name)} = p{snake_to_pascal_case(field_name)};\n"

        class_unique_fields += member_field_line

    class_constructor_line = f"\tpublic {snake_to_pascal_case(class_info_dict[g_message_name_attr_string_name])}({class_constructor_params[:-2]})\n\t{{\n{class_constructor_body}\t}}\n"
    class_constructor_default_line = f"\tpublic {snake_to_pascal_case(class_info_dict[g_message_name_attr_string_name])}() {{}}\n"

    # LOL
    class_ending_brace = "}"

    # Final file :DD
    class_file = class_heading + class_hellenic_interface_fields + class_unique_fields + class_constructor_line + class_constructor_default_line + class_ending_brace

    return class_file


def generate_dialect_files(input_XML_file_path, output_directory):
    tree = ET.parse(input_XML_file_path)

    messages = tree.getroot().find(g_messages_tag_string_name)

    # Dictionary for message will look like the following.
    # Contains all necessary information to generate a class definition
    # {
    #     "id": 0,
    #     "name": "LATITUDE_LONGITUDE",
    #     "description": "blah"
    #     "fields": {
    #
    #         "lat": {
    #             "type" : "float64"
    #             "description": "blah"
    #         },
    #         "lon": {
    #             "type" : "float64"
    #             "description": "blah"
    #         }
    #     }
    # }

    for message in messages:

        # Dictionary that contains all necessary info to generate a message structure
        class_info_dict = {}

        msg_id = message.get(g_message_id_attr_string_name)
        msg_name = message.get(g_message_name_attr_string_name)
        msg_description = message.find(g_message_description_tag_string_name)

        class_info_dict[g_message_id_attr_string_name] = msg_id
        class_info_dict[g_message_name_attr_string_name] = msg_name
        class_info_dict[g_message_description_tag_string_name] = msg_description.text

        class_info_dict[g_message_fields_tag_string_name] = {}
        fields = message.find(g_message_fields_tag_string_name)
        for field in fields:
            field_name = field.get(g_message_field_name_attr_string_name)
            field_type = field.get(g_message_field_type_attr_string_name)

            field_description = field.find(g_message_description_tag_string_name).text

            class_info_dict[g_message_fields_tag_string_name][field_name] = {}
            class_info_dict[g_message_fields_tag_string_name][field_name].update(
                {g_message_field_type_attr_string_name: f"{field_type}"})
            class_info_dict[g_message_fields_tag_string_name][field_name].update(
                {g_field_description_tag_string_name: f"{field_description}"})

        class_file_contents = generate_dialect_class(class_info_dict)

        output_path = os.path.join(output_directory, f"{snake_to_pascal_case(msg_name)}.cs")
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, "w") as f:
            f.write(class_file_contents)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate C# message dialect from an XML file")

    parser.add_argument("--input_XML", required=True, help="File path to XML to turn into C# messages")
    parser.add_argument("--output_dir", required=True, help="Output directory for the generated C# messages")

    args = parser.parse_args()

    generate_hellenic_interface_file(args.output_dir)
    generate_dialect_files(args.input_XML, args.output_dir)
