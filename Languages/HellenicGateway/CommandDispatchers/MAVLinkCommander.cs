using System.Net.Sockets;
using System.Threading.Tasks;
using Hermes.Languages.HellenicGateway.Commands;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers;

public class MAVLinkCommander
{

    private const int MAX_RETRIES = 3;
    private UdpClient m_machineClient = new UdpClient(14580);
    private MAVLink.MavlinkParse m_mavlinkParser = new MAVLink.MavlinkParse();
    private byte m_systemId = 255;

    // TODO::ARGYRASPIDES() { How is this going to work exactly? What if the vehicle is connected via TCP?
    // via serial? A developer just wants to "send to a vehicle" and shouldn't give a shit about what
    // link its using. Just send and forget. Make some "infrastructure" for this here ... }
    public async Task<MAVLink.MAVLinkMessage> SendCommandInt20(MAVLink.mavlink_command_int_t command)
    {
        // TODO::ARGYRASPIDES() { What about the other params? They should be configurable? What do they mean?
        // How do I cleanly allow a developer to send commands? }
        byte[] commandBytes = m_mavlinkParser.GenerateMAVLinkPacket20(
            MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
            command
        );

        // TODO::ARGYRASPIDES() {
        // Technically the MAVLinkAdapter is going to pick up the acknowledgement command. Thats not so good,
        // we should be doing retries and whatnot all inside here. I guess MAVLinkAdapter is only going to be
        // for telemetry? Then it should be called MAVLinkTelemetryAdapter? And this should be called MAVLinkCommandAdapter?
        // }
        m_machineClient.Send(commandBytes, commandBytes.Length);
        return null;
    }

    public async Task<MAVLink.MAVLinkMessage> SendCommandLong20(MAVLink.mavlink_command_long_t command)
    {
        return null;
    }


}
