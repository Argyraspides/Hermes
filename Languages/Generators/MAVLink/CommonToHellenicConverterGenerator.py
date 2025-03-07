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

# This script takes in an XML file that defines mappings from MAVLink messages to Hellenic messages
# The XML structure for mappings is assumed to be as follows (note how one MAVLink message may map to multiple Hellenic messages,
# and that some fields in Hellenic messages don't have a MAVLink equivalent, and thus are given a default value):

'''xml
<?xml version="1.0" encoding="utf-8"?>
<common_to_hellenic>
    <conversions>
        <message common_id="33" common_name="GLOBAL_POSITION_INT">
            <!-- LATITUDE_LONGITUDE Message (id=0) Mappings -->
            <mapping
                    common_name="lat"
                    common_type="int32_t"
                    common_units="degE7"
                    hellenic_id="0"
                    hellenic_name="lat"
                    hellenic_type="float64"
                    hellenic_units="degrees"
                    conversion="value / 10000000.0">
            </mapping>
            <mapping
                    common_name="lon"
                    common_type="int32_t"
                    common_units="degE7"
                    hellenic_id="0"
                    hellenic_name="lon"
                    hellenic_type="float64"
                    hellenic_units="degrees"
                    conversion="value / 10000000.0">
            </mapping>
            <mapping
                    common_name="time_boot_ms"
                    common_type="uint32_t"
                    common_units="ms"
                    hellenic_id="0"
                    hellenic_name="time_usec"
                    hellenic_type="uint64_t"
                    hellenic_units="μs"
                    conversion="ConvertBootMsToEpochUs(value)">
            </mapping>
            <default_value
                    hellenic_id="0"
                    hellenic_name="reference_frame"
                    hellenic_type="uint8_t"
                    value="2"><!-- 2 = Earth in your enum -->
            </default_value>

            <!-- ALTITUDE Message (id=1) Mappings -->
            <mapping
                    common_name="alt"
                    common_type="int32_t"
                    common_units="mm"
                    hellenic_id="1"
                    hellenic_name="alt"
                    hellenic_type="float64"
                    hellenic_units="m"
                    conversion="value / 1000.0">
            </mapping>
            <mapping
                    common_name="relative_alt"
                    common_type="int32_t"
                    common_units="mm"
                    hellenic_id="1"
                    hellenic_name="relative_alt"
                    hellenic_type="float64"
                    hellenic_units="m"
                    conversion="value / 1000.0">
            </mapping>
            <mapping
                    common_name="time_boot_ms"
                    common_type="uint32_t"
                    common_units="ms"
                    hellenic_id="1"
                    hellenic_name="time_usec"
                    hellenic_type="uint64_t"
                    hellenic_units="μs"
                    conversion="ConvertBootMsToEpochUs(value)">
            </mapping>

            <!-- GROUND_VELOCITY Message (id=2) Mappings -->
            <mapping
                    common_name="vx"
                    common_type="int16_t"
                    common_units="cm/s"
                    hellenic_id="2"
                    hellenic_name="vx"
                    hellenic_type="float64"
                    hellenic_units="m/s"
                    conversion="value / 100.0">
            </mapping>
            <mapping
                    common_name="vy"
                    common_type="int16_t"
                    common_units="cm/s"
                    hellenic_id="2"
                    hellenic_name="vy"
                    hellenic_type="float64"
                    hellenic_units="m/s"
                    conversion="value / 100.0">
            </mapping>
            <mapping
                    common_name="vz"
                    common_type="int16_t"
                    common_units="cm/s"
                    hellenic_id="2"
                    hellenic_name="vz"
                    hellenic_type="float64"
                    hellenic_units="m/s"
                    conversion="value / -100.0"><!-- Note: Inversion because MAVLink has positive down, Hellenic has positive up -->
            </mapping>
            <mapping
                    common_name="time_boot_ms"
                    common_type="uint32_t"
                    common_units="ms"
                    hellenic_id="2"
                    hellenic_name="time_usec"
                    hellenic_type="uint64_t"
                    hellenic_units="μs"
                    conversion="ConvertBootMsToEpochUs(value)">
            </mapping>

        </message>
    </conversions>
</common_to_hellenic>
'''

# The incoming JSON messages are assumed to be structured in the following way, and comes from the Python script over a WebSocket:

'''json
{
    "msgid" : 33,
    "sysid" : 1,
    "compid" : 1,
    "sequence" : 224,
    "payload" : {
        "mavpackettype" : "GLOBAL_POSITION_INT",
        "time_boot_ms" : 22299760,
        "lat" : 473979704,
        "lon" : 85461630,
        "alt" : -573,
        "relative_alt" : 319,
        "vx" : -4,
        "vy" : 0,
        "vz" : 25,
        "hdg" : 8282
    }
}
'''


def generate_conversion_function():
    pass


def generate_conversion_class():
    class_header = '''
using System.Text.Json.Nodes;

/*
We assume incoming MAVLink JSON messages come in like this:

{
    "msgid" : 33,
    "sysid" : 1,
    "compid" : 1,
    "sequence" : 224,
    "payload" : {
        "mavpackettype" : "GLOBAL_POSITION_INT",
        "time_boot_ms" : 22299760,
        "lat" : 473979704,
        "lon" : 85461630,
        "alt" : -573,
        "relative_alt" : 319,
        "vx" : -4,
        "vy" : 0,
        "vz" : 25,
        "hdg" : 8282
    }
}

*/
class MAVLinkToHellenicTranslator
{
    // Maps MAVLink message ID's to the respective functions that will translate them
    Dictionary<int, Func<JsonNode, List<IHellenicMessage>>> MAVLinkIdToConversionFunctionDict;
    '''


if __name__ == "__main__":

    if __name__ == "__main__":
        parser = argparse.ArgumentParser(description="Generate C# message dialect from an XML file")

    parser.add_argument("--input_XML", required=True, help="File path to XML to turn into C# messages")
    parser.add_argument("--output_dir", required=True, help="Output directory for the generated C# messages")

    args = parser.parse_args()
