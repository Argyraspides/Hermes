# import os
# import xml.etree.ElementTree as ET
# from typing import List, Dict
#
#
# class GDScriptGenerator:
#     def __init__(self, xml_path: str):
#         self.tree = ET.parse(xml_path)
#         self.root = self.tree.getroot()
#         self.message_ids = []  # Store message IDs and names for class map
#
#     def generate_enum(self, enum_elem: ET.Element) -> str:
#         """Generate GDScript enum from MAVLink enum definition"""
#         enum_name = enum_elem.attrib['name']
#         entries = []
#
#         for entry in enum_elem.findall('entry'):
#             name = entry.attrib['name']
#             value = entry.attrib['value']
#             description = entry.find('description')
#             desc_text = description.text if description is not None else ""
#
#             # Add comment if there's a description
#             if desc_text:
#                 commented_desc = desc_text.replace("\n", "\n# ")
#                 entries.append(f"    # {commented_desc}")
#             entries.append(f"    {name} = {value}")
#
#         enum_text = f"enum {enum_name} {{\n"
#         enum_text += ",\n".join(entries)
#         enum_text += "\n}"
#         return enum_text
#
#     def generate_message_class(self, message_elem: ET.Element) -> str:
#         """Generate GDScript class from MAVLink message definition"""
#         msg_name = message_elem.attrib['name']
#         msg_id = message_elem.attrib['id']
#
#         # Store message info for class map
#         self.message_ids.append((int(msg_id), msg_name))
#
#         description = message_elem.find('description')
#         desc_text = description.text if description is not None else ""
#
#         fields: List[Dict] = []
#         for field in message_elem.findall('field'):
#             field_data = {
#                 'name': field.attrib['name'],
#                 'type': field.attrib['type'],
#                 'description': field.text if field.text else ""
#             }
#             fields.append(field_data)
#
#         # Generate class
#         class_lines = []
#         if desc_text:
#             commented_desc = desc_text.replace("\n", "\n# ")
#             class_lines.append(f"# {commented_desc}")
#         class_lines.append(f"class_name {msg_name}")
#         class_lines.append("")
#         class_lines.append(f"const MESSAGE_ID = {msg_id}")
#         class_lines.append("")
#
#         # Generate properties
#         for field in fields:
#             field_type = self._convert_type_to_gdscript(field['type'])
#             if field['description']:
#                 commented_var_desc = field['description'].replace("\n", "\n# ")
#                 class_lines.append(f"# {commented_var_desc}")
#             class_lines.append(f"var {field['name']}: {field_type}")
#
#         return "\n".join(class_lines)
#
#     def generate_class_map(self) -> str:
#         """Generate a GDScript file containing the message ID to class mapping"""
#         # Sort messages by ID for readability
#         sorted_messages = sorted(self.message_ids, key=lambda x: x[0])
#
#         lines = []
#         lines.append("class_name MAVLinkMessageMap")
#         lines.append("")
#
#         # Generate the class map dictionary
#         lines.append("# Mapping from message IDs to message classes")
#         lines.append("var class_map: Dictionary = {")
#
#
#
#         # Write out dictionary entries with comments
#         for msg_id, msg_name in sorted_messages:
#             lines.append(f"    {msg_id}: {msg_name}, # {msg_name}")
#
#         lines.append("}")
#
#         return "\n".join(lines)
#
#     def _convert_type_to_gdscript(self, mavlink_type: str) -> str:
#         """Convert MAVLink types to GDScript types"""
#         type_mapping = {
#             'uint8_t': 'int',
#             'uint16_t': 'int',
#             'uint32_t': 'int',
#             'uint64_t': 'int',
#             'int8_t': 'int',
#             'int16_t': 'int',
#             'int32_t': 'int',
#             'int64_t': 'int',
#             'float': 'float',
#             'double': 'float',
#             'char': 'String',
#         }
#
#         # Handle array types
#         if '[' in mavlink_type:
#             base_type = mavlink_type.split('[')[0]
#             return f"Array[{type_mapping.get(base_type, 'Variant')}]"
#
#         return type_mapping.get(mavlink_type, 'Variant')
#
#     def generate_all(self, output_dir: str):
#         """Generate all GDScript code from the MAVLink XML and save to output_dir"""
#         # Ensure the output directory exists
#         os.makedirs(output_dir, exist_ok=True)
#
#         # Generate enums
#         for enum in self.root.findall(".//enum"):
#             enum_code = self.generate_enum(enum)
#             enum_name = enum.attrib['name']
#             with open(os.path.join(output_dir, f"{enum_name}.gd"), 'w') as f:
#                 f.write(enum_code)
#
#         # Generate message classes
#         for message in self.root.findall(".//message"):
#             message_code = self.generate_message_class(message)
#             msg_name = message.attrib['name']
#             with open(os.path.join(output_dir, f"{msg_name}.gd"), 'w') as f:
#                 f.write(message_code)
#
#         # Generate the class map after all messages have been processed
#         class_map_code = self.generate_class_map()
#         with open(os.path.join(output_dir, "MAVLinkMessageMap.gd"), 'w') as f:
#             f.write(class_map_code)
#
#
# def main():
#     # Define the path to the MAVLink XML and the output folder
#     xml_path = 'mavlink_common.xml'
#     output_dir = os.path.join(os.path.dirname(__file__), 'MAVLinkDefinitions')
#
#     # Create the generator instance
#     generator = GDScriptGenerator(xml_path)
#
#     # Generate the GDScript files into the specified directory
#     generator.generate_all(output_dir)
#
#
# if __name__ == '__main__':
#     main()


import os
import xml.etree.ElementTree as ET
from typing import List, Dict


class GDScriptGenerator:
    def __init__(self, xml_path: str):
        self.tree = ET.parse(xml_path)
        self.root = self.tree.getroot()
        self.message_ids = []  # Store message IDs and names for class map

    def generate_enum(self, enum_elem: ET.Element) -> str:
        """Generate GDScript enum from MAVLink enum definition"""
        enum_name = enum_elem.attrib['name']
        entries = []

        for entry in enum_elem.findall('entry'):
            name = entry.attrib['name']
            value = entry.attrib['value']
            description = entry.find('description')
            desc_text = description.text if description is not None else ""

            # Add comment if there's a description
            if desc_text:
                commented_desc = desc_text.replace("\n", "\n# ")
                entries.append(f"    # {commented_desc}")
            entries.append(f"    {name} = {value}")

        enum_text = f"enum {enum_name} {{\n"
        enum_text += ",\n".join(entries)
        enum_text += "\n}"
        return enum_text

    def generate_message_class(self, message_elem: ET.Element) -> str:
        msg_name = message_elem.attrib['name']
        msg_id = message_elem.attrib['id']

        self.message_ids.append((int(msg_id), msg_name))

        description = message_elem.find('description')
        desc_text = description.text if description is not None else ""

        # Track whether we've seen the extensions tag
        is_extension = False
        fields: List[Dict] = []

        for child in message_elem:
            if child.tag == 'extensions':
                is_extension = True
                continue

            if child.tag == 'field':
                field_data = {
                    'name': child.attrib['name'],
                    'type': child.attrib['type'],
                    'description': child.text if child.text else "",
                    'extension': is_extension
                }
                if 'enum' in child.attrib:
                    field_data['enum'] = child.attrib['enum']
                fields.append(field_data)

        # Generate class
        class_lines = []
        if desc_text:
            commented_desc = desc_text.replace("\n", "\n# ")
            class_lines.append(f"# {commented_desc}")
        class_lines.append(f"class_name {msg_name}")
        class_lines.append("")
        class_lines.append(f"const MESSAGE_ID = {msg_id}")
        class_lines.append("")

        # Generate field type mapping with extension information
        class_lines.append("# MAVLink field type information")
        class_lines.append("const FIELD_TYPES = {")
        for field in fields:
            mavlink_type = field['type']
            field_name = field['name']
            # Handle array types by extracting size if present
            if '[' in mavlink_type:
                base_type = mavlink_type.split('[')[0]
                array_size = mavlink_type.split('[')[1].strip(']')
                class_lines.append(
                    f'    "{field_name}": {{"type": "{base_type}", "array_length": {array_size}, "extension": {str(field["extension"]).lower()}}},')
            else:
                class_lines.append(
                    f'    "{field_name}": {{"type": "{mavlink_type}", "extension": {str(field["extension"]).lower()}}},')
        class_lines.append("}")
        class_lines.append("")

        # Generate enum mappings if present
        enum_fields = [f for f in fields if 'enum' in f]
        if enum_fields:
            class_lines.append("# Enum type information")
            class_lines.append("const ENUM_TYPES = {")
            for field in enum_fields:
                class_lines.append(f'    "{field["name"]}": "{field["enum"]}",')
            class_lines.append("}")
            class_lines.append("")

        # Generate properties
        for field in fields:
            field_type = self._convert_type_to_gdscript(field['type'])
            if field['description']:
                commented_var_desc = field['description'].replace("\n", "\n# ")
                class_lines.append(f"# {commented_var_desc}")
            class_lines.append(f"var {field['name']}: {field_type}")

        return "\n".join(class_lines)

    def _convert_type_to_gdscript(self, mavlink_type: str) -> str:
        type_mapping = {
            'uint8_t': 'int',
            'uint16_t': 'int',
            'uint32_t': 'int',
            'uint64_t': 'int',
            'int8_t': 'int',
            'int16_t': 'int',
            'int32_t': 'int',
            'int64_t': 'int',
            'float': 'float',
            'double': 'float',
            'char': 'String',
        }

        # Handle array types
        if '[' in mavlink_type:
            base_type = mavlink_type.split('[')[0]
            return f"Array[{type_mapping.get(base_type, 'Variant')}]"

        return type_mapping.get(mavlink_type, 'Variant')

    def generate_class_map(self) -> str:
        """Generate a GDScript file containing the message ID to class mapping"""
        sorted_messages = sorted(self.message_ids, key=lambda x: x[0])

        lines = []
        lines.append("class_name MAVLinkMessageMap")
        lines.append("")
        lines.append("# Mapping from message IDs to message classes")
        lines.append("var class_map: Dictionary = {")

        for msg_id, msg_name in sorted_messages:
            lines.append(f"    {msg_id}: {msg_name}, # {msg_name}")

        lines.append("}")

        return "\n".join(lines)

    def generate_all(self, output_dir: str):
        """Generate all GDScript code from the MAVLink XML and save to output_dir"""
        os.makedirs(output_dir, exist_ok=True)

        for enum in self.root.findall(".//enum"):
            enum_code = self.generate_enum(enum)
            enum_name = enum.attrib['name']
            with open(os.path.join(output_dir, f"{enum_name}.gd"), 'w') as f:
                f.write(enum_code)

        for message in self.root.findall(".//message"):
            message_code = self.generate_message_class(message)
            msg_name = message.attrib['name']
            with open(os.path.join(output_dir, f"{msg_name}.gd"), 'w') as f:
                f.write(message_code)

        class_map_code = self.generate_class_map()
        with open(os.path.join(output_dir, "MAVLinkMessageMap.gd"), 'w') as f:
            f.write(class_map_code)


def main():
    xml_path = 'mavlink_common.xml'
    output_dir = os.path.join(os.path.dirname(__file__), 'MAVLinkDefinitions')
    generator = GDScriptGenerator(xml_path)
    generator.generate_all(output_dir)


if __name__ == '__main__':
    main()