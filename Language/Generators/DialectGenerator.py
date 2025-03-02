#!/usr/bin/env python3
"""
DialectGenerator.py - Generates C# classes for Hellenic dialect messages
Usage: python DialectGenerator.py <hellenic_xml_path> <output_dir>
"""

import os
import sys
import xml.etree.ElementTree as ET
from textwrap import dedent, indent


class DialectGenerator:
    def __init__(self, hellenic_xml_path, output_dir):
        self.hellenic_xml_path = hellenic_xml_path
        self.output_dir = output_dir
        # C# type mapping from XML types
        self.type_mapping = {
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

    def parse_hellenic_xml(self):
        """Parse the Hellenic XML file and extract message definitions"""
        tree = ET.parse(self.hellenic_xml_path)
        root = tree.getroot()

        # Get version and dialect information
        version = root.find("version").text
        dialect = root.find("dialect").text

        # Extract message definitions
        messages = []
        for message_elem in root.find("messages").findall("message"):
            message = {
                "id": int(message_elem.attrib["id"]),
                "name": message_elem.attrib["name"],
                "description": message_elem.find("description").text.strip() if message_elem.find(
                    "description") is not None else "",
                "fields": []
            }

            # Extract field definitions
            for field_elem in message_elem.findall("field"):
                field = {
                    "type": field_elem.attrib["type"],
                    "name": field_elem.attrib["name"],
                    "units": field_elem.attrib.get("units", ""),
                    "description": field_elem.text.strip() if field_elem.text else ""
                }
                message["fields"].append(field)

            messages.append(message)

        return {
            "version": version,
            "dialect": dialect,
            "messages": sorted(messages, key=lambda m: m["id"])
        }

    def generate_interface(self):
        """Generate the IHellenicMessage interface"""
        return dedent("""
        using System;

        namespace Hermes.Dialect.Hellenic
        {
            /// <summary>
            /// Interface for all Hellenic dialect messages
            /// </summary>
            public interface IHellenicMessage
            {
                /// <summary>
                /// Message name
                /// </summary>
                string MessageName { get; }
            }
        }
        """).lstrip()

    def generate_message_class(self, message):
        """Generate a C# class for a Hellenic message"""
        # Format multiline description for C# XML comments
        description_lines = message['description'].strip().split('\n')
        formatted_description = "/// <summary>\n"
        for line in description_lines:
            formatted_description += f"    /// {line.strip()}\n"
        formatted_description += "    /// </summary>"

        # Class header and description
        cs_class = dedent(f"""
        using System;

        namespace Hermes.Dialect.Hellenic
        {{
            {formatted_description}
            public class {message['name']} : IHellenicMessage
            {{
                /// <summary>
                /// Message ID
                /// </summary>
                public static int MessageId => {message['id']};

                /// <summary>
                /// Message name
                /// </summary>
                public string MessageName => "{message['name']}";
        """).lstrip()

        # Add properties for each field
        for field in message["fields"]:
            cs_type = self.type_mapping.get(field["type"], "object")

            # Format multiline field description for C# XML comments
            field_desc_lines = field['description'].strip().split('\n')
            field_desc = ""
            for i, line in enumerate(field_desc_lines):
                unit_suffix = f" [{field['units']}]" if i == 0 and field['units'] else ""
                field_desc += f"                /// {line.strip()}{unit_suffix}\n"

            property_def = f"""
                /// <summary>
{field_desc.rstrip()}
                /// </summary>
                public {cs_type} {self.pascal_case(field['name'])} {{ get; set; }}
            """
            cs_class += "\n" + property_def

        # Add constructor
        cs_class += dedent(f"""
                /// <summary>
                /// Default constructor
                /// </summary>
                public {message['name']}() {{ }}
            }}
        }}
        """)

        return cs_class

    def pascal_case(self, snake_case):
        """Convert snake_case to PascalCase"""
        return ''.join(word.title() for word in snake_case.split('_'))

    def generate(self):
        """Generate C# classes for all Hellenic messages"""
        hellenic_data = self.parse_hellenic_xml()

        # Create output directory if not exists
        hellenic_dir = os.path.join(self.output_dir, "Hellenic")
        os.makedirs(hellenic_dir, exist_ok=True)

        # Generate interface
        interface_path = os.path.join(hellenic_dir, "IHellenicMessage.cs")
        with open(interface_path, "w") as f:
            f.write(self.generate_interface())

        # Generate message classes
        for message in hellenic_data["messages"]:
            class_path = os.path.join(hellenic_dir, f"{message['name']}.cs")
            with open(class_path, "w") as f:
                f.write(self.generate_message_class(message))

        print(f"Generated {len(hellenic_data['messages'])} Hellenic message classes in {hellenic_dir}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python DialectGenerator.py <hellenic_xml_path> <output_dir>")
        sys.exit(1)

    hellenic_xml_path = sys.argv[1]
    output_dir = sys.argv[2]

    generator = DialectGenerator(hellenic_xml_path, output_dir)
    generator.generate()
