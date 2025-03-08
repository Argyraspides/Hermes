using System;
using System.Text.Json.Nodes;
using Godot;
using Hermes.Languages.HellenicGateway.StateMachines;

namespace Hermes.Languages.HellenicGateway.Adapters;

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

public class MAVLinkAdapter : IProtocolAdapter
{
    private MAVLinkToHellenicTranslator m_mavlinkToHellenicTranslator;
    private MAVLinkStateMachine m_mavlinkStateMachine;

    public bool IsOfProtocolType(byte[] rawPacket)
    {
        if (rawPacket == null || rawPacket.Length == 0) return false;

        // TODO::ARGYRASPIDES() { Don't do this here. Replacing "NaN" should be done at source (MAVLinkInterface.py) }
        string packetAsString = System.Text.Encoding.UTF8.GetString(rawPacket).Replace("NaN", "\"NaN\"");
        try
        {
            JsonNode node = JsonNode.Parse(packetAsString);
            return node["payload"]?["mavpackettype"] != null;
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message);
        }

        return false;
    }

    public void HandleMessage(byte[] rawPacket)
    {
    }
}
