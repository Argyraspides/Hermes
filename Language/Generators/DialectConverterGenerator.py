#!/usr/bin/env python3
"""
DialectConverterGenerator.py - Generates C# classes for converting MAVLink messages to Hellenic format
Usage: python DialectConverterGenerator.py <common_xml_path> <hellenic_xml_path> <common_to_hellenic_xml_path> <output_dir>
"""

import os
import sys
import xml.etree.ElementTree as ET
from textwrap import dedent


class DialectConverterGenerator:
    def __init__(self, common_xml_path, hellenic_xml_path, common_to_hellenic_xml_path, output_dir):
        self.common_xml_path = common_xml_path
        self.hellenic_xml_path = hellenic_xml_path
        self.common_to_hellenic_xml_path = common_to_hellenic_xml_path
        self.output_dir = output_dir
        self.common_messages = {}
        self.hellenic_messages = {}
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

    def parse_common_xml(self):
        """Parse the MAVLink common.xml file"""
        tree = ET.parse(self.common_xml_path)
        root = tree.getroot()

        for message_elem in root.findall(".//message"):
            message_id = int(message_elem.attrib["id"])
            message_name = message_elem.attrib["name"]

            fields = {}
            for field_elem in message_elem.findall("field"):
                field_name = field_elem.attrib["name"]
                field_type = field_elem.attrib["type"]
                field_units = field_elem.attrib.get("units", "")

                fields[field_name] = {
                    "name": field_name,
                    "type": field_type,
                    "units": field_units
                }

            self.common_messages[message_id] = {
                "id": message_id,
                "name": message_name,
                "fields": fields
            }

    def parse_hellenic_xml(self):
        """Parse the Hellenic dialect XML file"""
        tree = ET.parse(self.hellenic_xml_path)
        root = tree.getroot()

        for message_elem in root.find("messages").findall("message"):
            message_id = int(message_elem.attrib["id"])
            message_name = message_elem.attrib["name"]

            self.hellenic_messages[message_id] = {
                "id": message_id,
                "name": message_name
            }

    def parse_common_to_hellenic_xml(self):
        """Parse the mapping XML file"""
        tree = ET.parse(self.common_to_hellenic_xml_path)
        root = tree.getroot()

        converters = []

        for message_elem in root.findall("./conversions/message"):
            common_id = int(message_elem.attrib["common_id"])
            common_name = message_elem.attrib.get("common_name",
                                                  self.common_messages.get(common_id, {}).get("name", ""))

            # Group mappings by hellenic_id
            hellenic_mappings = {}

            # Process regular mappings
            for mapping_elem in message_elem.findall("mapping"):
                hellenic_id = int(mapping_elem.attrib["hellenic_id"])

                if hellenic_id not in hellenic_mappings:
                    hellenic_mappings[hellenic_id] = []

                mapping = {
                    "common_name": mapping_elem.attrib["common_name"],
                    "common_type": mapping_elem.attrib["common_type"],
                    "common_units": mapping_elem.attrib.get("common_units", ""),
                    "hellenic_id": hellenic_id,
                    "hellenic_name": mapping_elem.attrib["hellenic_name"],
                    "hellenic_type": mapping_elem.attrib["hellenic_type"],
                    "hellenic_units": mapping_elem.attrib.get("hellenic_units", ""),
                    "conversion": mapping_elem.attrib.get("conversion", "value"),
                    "is_default": False
                }

                hellenic_mappings[hellenic_id].append(mapping)

            # Process default values
            for default_elem in message_elem.findall("default_value"):
                hellenic_id = int(default_elem.attrib["hellenic_id"])

                if hellenic_id not in hellenic_mappings:
                    hellenic_mappings[hellenic_id] = []

                default = {
                    "hellenic_id": hellenic_id,
                    "hellenic_name": default_elem.attrib["hellenic_name"],
                    "hellenic_type": default_elem.attrib["hellenic_type"],
                    "value": default_elem.attrib["value"],
                    "is_default": True
                }

                hellenic_mappings[hellenic_id].append(default)

            converter = {
                "common_id": common_id,
                "common_name": common_name,
                "hellenic_mappings": hellenic_mappings
            }

            converters.append(converter)

        return converters

    def generate_converter_interface(self):
        """Generate the IMAVLinkConverter interface"""
        return dedent("""
        using System;
        using System.Collections.Generic;
        using Newtonsoft.Json.Linq;
        using Hermes.Dialect.Hellenic;

        namespace Hermes.Conversion
        {
            /// <summary>
            /// Interface for all MAVLink to Hellenic converters
            /// </summary>
            public interface IMAVLinkConverter
            {
                /// <summary>
                /// MAVLink message ID this converter handles
                /// </summary>
                int MAVLinkMessageId { get; }

                /// <summary>
                /// MAVLink message name this converter handles
                /// </summary>
                string MAVLinkMessageName { get; }

                /// <summary>
                /// Convert a MAVLink JSON message to Hellenic message(s)
                /// </summary>
                /// <param name="jsonObj">The JSON object containing the MAVLink message</param>
                /// <returns>A list of Hellenic messages created from the MAVLink message</returns>
                List<IHellenicMessage> Convert(JObject jsonObj);
            }
        }
        """).lstrip()

    def generate_converter_class(self, converter):
        """Generate a converter class for a specific MAVLink message"""
        common_name = converter["common_name"]
        hellenic_mappings = converter["hellenic_mappings"]

        class_name = f"{common_name}_Converter"

        # Start with class declaration and header
        cs_class = dedent(f"""
        using System;
        using System.Collections.Generic;
        using Newtonsoft.Json.Linq;
        using Hermes.Dialect.Hellenic;

        namespace Hermes.Conversion
        {{
            /// <summary>
            /// Converts MAVLink {common_name} messages to Hellenic format
            /// </summary>
            public class {class_name} : IMAVLinkConverter
            {{
                /// <summary>
                /// MAVLink message ID this converter handles
                /// </summary>
                public int MAVLinkMessageId => {converter['common_id']};

                /// <summary>
                /// MAVLink message name this converter handles
                /// </summary>
                public string MAVLinkMessageName => "{common_name}";

                /// <summary>
                /// Convert a MAVLink JSON message to Hellenic message(s)
                /// </summary>
                /// <param name="jsonObj">The JSON object containing the MAVLink message</param>
                /// <returns>A list of Hellenic messages created from the MAVLink message</returns>
                public List<IHellenicMessage> Convert(JObject jsonObj)
                {{
                    // Get the payload from the JSON
                    JObject payload = (JObject)jsonObj["payload"];

                    // Create a list to hold the converted messages
                    List<IHellenicMessage> messages = new List<IHellenicMessage>();
        """).lstrip()

        # Generate conversion code for each Hellenic message
        for hellenic_id, mappings in hellenic_mappings.items():
            hellenic_name = self.hellenic_messages.get(hellenic_id, {}).get("name", f"Unknown{hellenic_id}")

            cs_class += dedent(f"""
                    // Create {hellenic_name} message
                    var {self.camel_case(hellenic_name)} = new {hellenic_name}();
            """)

            # Add field assignments
            for mapping in mappings:
                if mapping["is_default"]:
                    # Handle default value
                    hellenic_name_field = mapping["hellenic_name"]
                    value = mapping["value"]

                    cs_class += dedent(f"""
                    // Set default value
                    {self.camel_case(hellenic_name)}.{self.pascal_case(hellenic_name_field)} = {value};
                    """)
                else:
                    # Handle field mapping
                    common_name_field = mapping["common_name"]
                    hellenic_name_field = mapping["hellenic_name"]
                    conversion = mapping["conversion"]
                    cs_type = self.map_type_to_cs(mapping["common_type"])

                    # Handle time conversion specially
                    if common_name_field == "time_boot_ms" and hellenic_name_field == "time_usec":
                        cs_class += dedent(f"""
                    // Convert boot time to epoch microseconds
                    if (payload["{common_name_field}"] != null)
                    {{
                        uint bootMs = payload["{common_name_field}"].Value<uint>();
                        {self.camel_case(hellenic_name)}.{self.pascal_case(hellenic_name_field)} = ConvertBootMsToEpochUs(bootMs);
                    }}
                        """)
                    else:
                        # Standard field conversion
                        cs_class += dedent(f"""
                    // Convert {common_name_field} to {hellenic_name_field}
                    if (payload["{common_name_field}"] != null)
                    {{
                        {cs_type} value = payload["{common_name_field}"].Value<{cs_type}>();
                        {self.camel_case(hellenic_name)}.{self.pascal_case(hellenic_name_field)} = {self.apply_conversion(conversion, cs_type)};
                    }}
                        """)

            # Add the message to the list
            cs_class += dedent(f"""
                    // Add the message to the result list
                    messages.Add({self.camel_case(hellenic_name)});
            """)

        # Add helper methods and close the class
        cs_class += dedent("""
                    return messages;
                }

                /// <summary>
                /// Converts boot time in milliseconds to epoch time in microseconds
                /// </summary>
                /// <param name="bootMs">Time since boot in milliseconds</param>
                /// <returns>Time since epoch in microseconds</returns>
                private ulong ConvertBootMsToEpochUs(uint bootMs)
                {
                    // TODO: Implement this method based on your system's boot time tracking
                    // This is a placeholder implementation
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                    // This assumes you have a way to know when the system booted
                    // For now, we'll just use the current time minus bootMs
                    DateTime bootTime = DateTime.UtcNow.AddMilliseconds(-bootMs);
                    TimeSpan timeSinceEpoch = bootTime.Subtract(epoch);

                    return (ulong)(timeSinceEpoch.TotalMilliseconds * 1000);
                }
            }
        }
        """)

        return cs_class

    def generate_converter_factory(self, converters):
        """Generate a factory class to manage all converters"""
        # Start with class declaration and imports
        factory_class = dedent("""
        using System;
        using System.Collections.Generic;
        using Newtonsoft.Json.Linq;
        using Hermes.Dialect.Hellenic;

        namespace Hermes.Conversion
        {
            /// <summary>
            /// Factory that creates and manages MAVLink to Hellenic converters
            /// </summary>
            public class MAVLinkConverterFactory
            {
                private readonly Dictionary<int, IMAVLinkConverter> _convertersByMsgId = new Dictionary<int, IMAVLinkConverter>();
                private readonly Dictionary<string, IMAVLinkConverter> _convertersByName = new Dictionary<string, IMAVLinkConverter>();

                /// <summary>
                /// Initialize the converter factory with all available converters
                /// </summary>
                public MAVLinkConverterFactory()
                {
                    RegisterConverters();
                }

                /// <summary>
                /// Register all available converters
                /// </summary>
                private void RegisterConverters()
                {
        """).lstrip()

        # Register each converter
        for converter in converters:
            common_name = converter["common_name"]
            factory_class += f"            RegisterConverter(new {common_name}_Converter());\n"

        # Add methods for registration and conversion
        factory_class += dedent("""
                }

                /// <summary>
                /// Register a converter
                /// </summary>
                /// <param name="converter">The converter to register</param>
                private void RegisterConverter(IMAVLinkConverter converter)
                {
                    _convertersByMsgId[converter.MAVLinkMessageId] = converter;
                    _convertersByName[converter.MAVLinkMessageName] = converter;
                }

                /// <summary>
                /// Get a converter by MAVLink message ID
                /// </summary>
                /// <param name="msgId">The MAVLink message ID</param>
                /// <returns>The converter, or null if not found</returns>
                public IMAVLinkConverter GetConverterByMsgId(int msgId)
                {
                    if (_convertersByMsgId.TryGetValue(msgId, out var converter))
                    {
                        return converter;
                    }

                    return null;
                }

                /// <summary>
                /// Get a converter by MAVLink message name
                /// </summary>
                /// <param name="msgName">The MAVLink message name</param>
                /// <returns>The converter, or null if not found</returns>
                public IMAVLinkConverter GetConverterByName(string msgName)
                {
                    if (_convertersByName.TryGetValue(msgName, out var converter))
                    {
                        return converter;
                    }

                    return null;
                }

                /// <summary>
                /// Convert a MAVLink JSON message to Hellenic messages
                /// </summary>
                /// <param name="jsonObj">The JSON object containing the MAVLink message</param>
                /// <returns>A list of Hellenic messages, or an empty list if no converter found</returns>
                public List<IHellenicMessage> Convert(JObject jsonObj)
                {
                    // Try to get message ID
                    if (jsonObj["msgid"] != null)
                    {
                        int msgId = jsonObj["msgid"].Value<int>();
                        var converter = GetConverterByMsgId(msgId);

                        if (converter != null)
                        {
                            return converter.Convert(jsonObj);
                        }
                    }

                    // Try to get message name from payload
                    if (jsonObj["payload"] != null && jsonObj["payload"]["mavpackettype"] != null)
                    {
                        string msgName = jsonObj["payload"]["mavpackettype"].Value<string>();
                        var converter = GetConverterByName(msgName);

                        if (converter != null)
                        {
                            return converter.Convert(jsonObj);
                        }
                    }

                    // No converter found
                    return new List<IHellenicMessage>();
                }
            }
        }
        """)

        return factory_class

    def map_type_to_cs(self, mavlink_type):
        """Map MAVLink types to C# types"""
        return self.type_mapping.get(mavlink_type, "object")

    def apply_conversion(self, conversion_str, cs_type):
        """Apply a conversion expression to a value"""
        if conversion_str == "value":
            return "value"
        elif "ConvertBootMsToEpochUs" in conversion_str:
            # Special case for time conversion function
            return conversion_str
        else:
            # Standard numeric conversion
            return conversion_str

    def pascal_case(self, snake_case):
        """Convert snake_case to PascalCase"""
        return ''.join(word.title() for word in snake_case.split('_'))

    def camel_case(self, pascal_case):
        """Convert PascalCase to camelCase"""
        if not pascal_case:
            return ""
        return pascal_case[0].lower() + pascal_case[1:].lower()

    def generate(self):
        """Generate all converter classes"""
        self.parse_common_xml()
        self.parse_hellenic_xml()
        converters = self.parse_common_to_hellenic_xml()

        # Create output directory if not exists
        conversion_dir = os.path.join(self.output_dir, "Conversion")
        os.makedirs(conversion_dir, exist_ok=True)

        # Generate interface
        interface_path = os.path.join(conversion_dir, "IMAVLinkConverter.cs")
        with open(interface_path, "w") as f:
            f.write(self.generate_converter_interface())

        # Generate converter classes
        for converter in converters:
            class_path = os.path.join(conversion_dir, f"{converter['common_name']}_Converter.cs")
            with open(class_path, "w") as f:
                f.write(self.generate_converter_class(converter))

        # Generate factory class
        factory_path = os.path.join(conversion_dir, "MAVLinkConverterFactory.cs")
        with open(factory_path, "w") as f:
            f.write(self.generate_converter_factory(converters))

        print(f"Generated {len(converters)} MAVLink to Hellenic converters in {conversion_dir}")


if __name__ == "__main__":
    if len(sys.argv) != 5:
        print(
            "Usage: python DialectConverterGenerator.py <common_xml_path> <hellenic_xml_path> <common_to_hellenic_xml_path> <output_dir>")
        sys.exit(1)

    common_xml_path = sys.argv[1]
    hellenic_xml_path = sys.argv[2]
    common_to_hellenic_xml_path = sys.argv[3]
    output_dir = sys.argv[4]

    generator = DialectConverterGenerator(common_xml_path, hellenic_xml_path, common_to_hellenic_xml_path, output_dir)
    generator.generate()
