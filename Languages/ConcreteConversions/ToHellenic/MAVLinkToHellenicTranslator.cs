using System;
using System.Collections.Generic;
using System.IO;
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

    public static List<IHellenicMessage> TranslateMAVLinkMessage(JsonNode jsonDocument)
    {
        // Extract the message ID
        int msgId = jsonDocument["msgid"].GetValue<int>();

        // Look up the appropriate conversion function
        if (MAVLinkIdToConversionFunctionDict.TryGetValue(msgId, out var conversionFunc))
        {
            return conversionFunc(jsonDocument);
        }

        // Certified bruh moment
        throw new InvalidDataException("Unable to translate MAVLink message! No suitable translation function found ... the MAVLink message might have herpes!");
    }

    public static List<IHellenicMessage> GlobalPositionIntToHellenic(JsonNode jsonDocument)
    {
        var payload = jsonDocument["payload"];

        // Apply conversions from the XML mapping and use constructors for each message type
        var LatitudeLongitudeHellenicMessage = new LatitudeLongitude(
            pLat: payload["lat"].GetValue<int>() / 10000000.0,
            pLon: payload["lon"].GetValue<int>() / 10000000.0,
            pTimeUsec: payload["time_boot_ms"].GetValue<uint>(),
            pReferenceFrame: 2
        );

        var AltitudeHellenicMessage = new Altitude(
            pAlt: payload["alt"].GetValue<int>() / 1000.0,
            pRelativeAlt: payload["relative_alt"].GetValue<int>() / 1000.0,
            pTimeUsec: payload["time_boot_ms"].GetValue<uint>()
        );

        var GroundVelocityHellenicMessage = new GroundVelocity(
            pVx: payload["vx"].GetValue<short>() / 100.0,
            pVy: payload["vy"].GetValue<short>() / 100.0,
            pVz: payload["vz"].GetValue<short>() / -100.0,
            pTimeUsec: payload["time_boot_ms"].GetValue<uint>()
        );

        var HeadingHellenicMessage = new Heading(
            pHdg: payload["hdg"].GetValue<ushort>() / 100.0,
            pTimeUsec: payload["time_boot_ms"].GetValue<uint>(),
            pReferenceFrame: 2
        );

        return new List<IHellenicMessage> {
            LatitudeLongitudeHellenicMessage,
            AltitudeHellenicMessage,
            GroundVelocityHellenicMessage,
            HeadingHellenicMessage
        };
    }

    public static Dictionary<int, Func<JsonNode, List<IHellenicMessage>>> MAVLinkIdToConversionFunctionDict
    =
    new Dictionary<int, Func<JsonNode, List<IHellenicMessage>>>()
    {
        {33, GlobalPositionIntToHellenic}
    };
}