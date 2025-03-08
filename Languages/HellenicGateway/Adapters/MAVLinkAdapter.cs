namespace Hermes.Languages.HellenicGateway.Adapters;

using System.Collections.Generic;
using System.IO;
using Hermes.Languages.HellenicGateway.StateMachines;

public class MAVLinkAdapter : IProtocolAdapter
{
    private MAVLinkStateMachine m_mavlinkStateMachine = new MAVLinkStateMachine();
    private MAVLink.MavlinkParse m_parser = new MAVLink.MavlinkParse();

    public bool IsOfProtocolType(byte[] rawPacket)
    {
        if (rawPacket == null || rawPacket.Length < 1)
            return false;
        return rawPacket[0] == MAVLink.MAVLINK_STX || rawPacket[0] == MAVLink.MAVLINK_STX_MAVLINK1;
    }

    public List<HellenicMessage> HandleMessage(byte[] rawPacket)
    {
        List<HellenicMessage> hellenicMessages = null;
        using (MemoryStream memStream = new MemoryStream(rawPacket))
        {
            MAVLink.MAVLinkMessage fullMAVLinkMessage = m_parser.ReadPacket(memStream);
            if (fullMAVLinkMessage == null)
            {
                return hellenicMessages;
            }

            hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(fullMAVLinkMessage);

            switch (fullMAVLinkMessage.msgid)
            {
                case (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                    MAVLink.mavlink_heartbeat_t heartbeatMessage =
                        MavlinkUtil.ByteArrayToStructure<MAVLink.mavlink_heartbeat_t>(rawPacket);
                    m_mavlinkStateMachine.HandleHeartBeatMessage(fullMAVLinkMessage, heartbeatMessage);
                    break;
                case (uint)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                    MAVLink.mavlink_global_position_int_t globalPositionIntMsg =
                        MavlinkUtil.ByteArrayToStructure<MAVLink.mavlink_global_position_int_t>(rawPacket);
                    m_mavlinkStateMachine.HandleGlobalPositionIntMessage(fullMAVLinkMessage, globalPositionIntMsg);
                    break;
            }
        }

        return hellenicMessages;
    }
}
