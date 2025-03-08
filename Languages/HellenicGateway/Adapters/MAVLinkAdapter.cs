using System.IO;
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
    private MAVLink.MavlinkParse m_parser;


    public bool IsOfProtocolType(byte[] rawPacket)
    {
        if (rawPacket == null || rawPacket.Length < 1)
            return false;
        return rawPacket[0] == MAVLink.MAVLINK_STX || rawPacket[0] == MAVLink.MAVLINK_STX_MAVLINK1;
    }

    public void HandleMessage(byte[] rawPacket)
    {
        using (MemoryStream memStream = new MemoryStream(rawPacket))
        {
            MAVLink.MAVLinkMessage fullMAVLinkMessage = m_parser.ReadPacket(memStream);
            if (fullMAVLinkMessage == null)
            {
                return;
            }

            switch (fullMAVLinkMessage.msgid)
            {
                case (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                    MAVLink.mavlink_heartbeat_t heartbeatMessage =
                        MavlinkUtil.ByteArrayToStructure<MAVLink.mavlink_heartbeat_t>(rawPacket);
                    m_mavlinkStateMachine.HandleHeartBeatMessage(fullMAVLinkMessage, heartbeatMessage);
                    break;
            }
        }
    }
}
