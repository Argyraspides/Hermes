using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hermes.Common.HermesUtils;
using Hermes.Common.Networking.UDP;
using Hermes.Core.Machine;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

public class MAVLinkCommander
{

    private const double LAT_LON_SCALE_FACTOR = 1e7;
    private const uint FORCE_ARM_VALUE = 21196;
    private const uint MAX_NORMAL_WAIT_TIME_MS = 1500;
    private const uint IN_PROGRESS_WAIT_TIME_MS = 3000;
    private const uint MAX_RETRIES = 3;

    private byte GCS_MAVLINK_ID = 255;

    private int MAVLINK_UDP_RECIEVE_PORT = 14445;
    private int MAVLINK_UDP_DST_CMD_PORT = 14580;
    private int HERMES_UDP_SRC_PORT = 11777;

    private global::MAVLink.MavlinkParse mavlinkParser = new global::MAVLink.MavlinkParse();
    // TODO::ARGYRASPIDES() { Figure out how you're gonna manage different links coz its not always gonna be UDP lol }
    private UdpClient sender;

    private uint receiverId;
    private IPEndPoint receiverEndPoint;

    public MAVLinkCommander()
    {
        sender = new UdpClient(HERMES_UDP_SRC_PORT);

        receiverEndPoint = IPEndPoint.Parse($"127.0.0.1:{MAVLINK_UDP_RECIEVE_PORT}");
        receiverId = HermesUdpClient.RegisterUdpClient(receiverEndPoint);

    }

    ~MAVLinkCommander()
    {
        sender.Close();
        sender.Dispose();

        HermesUdpClient.DeregisterUdpClient(receiverId, receiverEndPoint);
    }

    public async Task<bool> SendMAVLinkTakeoffCommand(Machine machine, double altitude, double pitch = 0.0d, double yaw = 0.0d)
    {
        if (machine == null)
        {
            HermesUtils.HermesLogError($"Cannot send takeoff command -- null vehilce.");
            return false;
        }

       HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.LatitudeLongitude);

       if (msg == null || msg is not LatitudeLongitude latlon)
       {
           HermesUtils.HermesLogError($"Cannot send takeoff command -- vehicle lat/lon unknown. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
           return false;
       }

       if (!latlon.Lat.HasValue || !latlon.Lon.HasValue)
       {
           HermesUtils.HermesLogError($"Cannot send takeoff command -- vehicle lat/lon unknown. MachineID: {machine.MachineId}");
           return false;
       }

       if (!machine.MachineId.HasValue)
       {
           HermesUtils.HermesLogError("Cannot send takeoff command -- vehicle machineId unknown");
           return false;
       }

       int lat = (int)(latlon.Lat.Value * LAT_LON_SCALE_FACTOR);
       int lon = (int)(latlon.Lon.Value * LAT_LON_SCALE_FACTOR);

       for (int i = 0; i < MAX_RETRIES; i++)
       {
            // Should be a mavlink_command_int_t coz we can supply like a reference frame and stuff
            global::MAVLink.mavlink_command_int_t mavlinkCommand = new global::MAVLink.mavlink_command_int_t
            {
                param1 = (float)pitch,
                param2 = float.NaN,
                param3 = float.NaN,
                param4 = (float)yaw,
                x = lat,
                y = lon,
                z = (float)altitude,
                command = (ushort)global::MAVLink.MAV_CMD.TAKEOFF,
                target_system = (byte)machine.MachineId.Value,
                target_component = (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                frame = (byte)global::MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT,
                current = byte.MinValue,
                autocontinue = byte.MinValue
            };

            byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                global::MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
                mavlinkCommand,
                false,
                GCS_MAVLINK_ID,
                (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                0
            );

            sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
            bool success = await AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.TAKEOFF);
            if (success)
            {
                HermesUtils.HermesLogSuccess($"Successfully performed take off command for machine #{machine.MachineId}");
                return true;
            }
       }

       HermesUtils.HermesLogError($"Unable to send MAVLink TAKEOFF command after {MAX_RETRIES} attempts. MachineID: {machine.MachineId}, Alt: {altitude}m");
       return false;
    }

    public async Task<bool> SendMAVLinkArmCommand(Machine machine, bool forceArm = false)
    {

        if (machine == null || !machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError($"Cannot send ARM command to null vehicle/vehicle without an ID. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
            return false;
        }

        for (int i = 0; i < MAX_RETRIES; i++)
        {
            // Should be a mavlink_command_int_t coz we can supply like a reference frame and stuff
            global::MAVLink.mavlink_command_long_t mavlinkCommand = new global::MAVLink.mavlink_command_long_t
            {
                target_system = (byte)machine.MachineId.Value,
                target_component = (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                command = (ushort)global::MAVLink.MAV_CMD.COMPONENT_ARM_DISARM,
                confirmation = 0,
                param1 = 1,
                param2 = forceArm ? FORCE_ARM_VALUE : 0,
                param3 = float.NaN,
                param4 = float.NaN,
                param5 = float.NaN,
                param6 = float.NaN,
                param7 = float.NaN
            };

            byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                global::MAVLink.MAVLINK_MSG_ID.COMMAND_LONG,
                mavlinkCommand,
                false,
                GCS_MAVLINK_ID,
                (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                0
            );

            sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));

            bool success = await AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.COMPONENT_ARM_DISARM);
            if (success)
            {
                HermesUtils.HermesLogSuccess($"Successfully performed arm command for machine #{machine.MachineId}");
                return true;
            }
            HermesUtils.HermesLogInfo($"Timed out waiting for ARM_DISARM command acknowledgement of machine #{machine.MachineId}. Attempt #{i + 1} of {MAX_RETRIES - 1} attempts.");
        }

        HermesUtils.HermesLogError($"Unable to send MAVLink COMPONENT_ARM_DISARM command after {MAX_RETRIES} attempts. MachineID: {machine.MachineId}, ForceArm: {forceArm}");
        return false;
    }

    public async Task<bool> SendMAVLinkLandCommand(
        Machine machine,
        double abortAlt = 0.0d,
        global::MAVLink.PRECISION_LAND_MODE landmode = 0,
        double yaw = 0.0d,
        double altitude = 0.0d)
    {

        HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.LatitudeLongitude);
        if (msg == null || msg is not LatitudeLongitude latlon)
        {
            HermesUtils.HermesLogError($"Cannot send land command -- vehicle lat/lon unknown. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
            return false;
        }

        if (!latlon.Lat.HasValue || !latlon.Lon.HasValue)
        {
            HermesUtils.HermesLogError($"Cannot send land command -- vehicle lat/lon unknown. MachineID: {machine.MachineId}");
            return false;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot send land command -- vehicle machineId unknown");
            return false;
        }

        int lat = (int)(latlon.Lat.Value * LAT_LON_SCALE_FACTOR);
        int lon = (int)(latlon.Lon.Value * LAT_LON_SCALE_FACTOR);

        for (int i = 0; i < MAX_RETRIES; i++)
        {
            global::MAVLink.mavlink_command_int_t mavlinkCommand = new global::MAVLink.mavlink_command_int_t
            {
                param1 = (float)abortAlt,
                param2 = (float)landmode,
                param3 = float.NaN,
                param4 = (float)yaw,
                x = lat,
                y = lon,
                z = (float)altitude,
                command = (ushort)global::MAVLink.MAV_CMD.LAND,
                target_system = (byte)machine.MachineId.Value,
                target_component = (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                frame = (byte)global::MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT,
                current = byte.MinValue,
                autocontinue = byte.MinValue
            };

            byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                global::MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
                mavlinkCommand,
                false,
                GCS_MAVLINK_ID,
                (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                0
            );

            sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
            bool success = await AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.LAND);
            if (success)
            {
                HermesUtils.HermesLogSuccess($"Successfully performed land command for machine #{machine.MachineId}");
                return true;
            }
            HermesUtils.HermesLogInfo($"Timed out waiting for LAND command acknowledgement of machine #{machine.MachineId}. Attempt #{i + 1} of {MAX_RETRIES - 1} attempts.");

        }

        HermesUtils.HermesLogError($"Unable to send MAVLink LAND command after {MAX_RETRIES} attempts. MachineID: {machine.MachineId}, AbortAlt: {abortAlt}m");
        return false;
    }

    public async Task<bool> AwaitMAVLinkAcknowledgement(Machine machine, global::MAVLink.MAV_CMD cmd)
    {

        if (machine == null)
        {
            HermesUtils.HermesLogError($"Cannot await for an acknowledgement of a null vehicle.");
            return false;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot wait for an acknowledgement of a vehicle without an ID");
            return false;
        }

        var initialTimeout = TimeSpan.FromMilliseconds(MAX_NORMAL_WAIT_TIME_MS);
        var extendedTimeout = TimeSpan.FromMilliseconds(IN_PROGRESS_WAIT_TIME_MS);

        var future = DateTime.Now.Add(initialTimeout);
        bool receivedInProgress = false;

        while(DateTime.Now < future)
        {
            var dat = await HermesUdpClient.ReceiveAsync(receiverId, receiverEndPoint);
            using (MemoryStream stream = new MemoryStream(dat.Buffer))
            {
                global::MAVLink.MAVLinkMessage message = mavlinkParser.ReadPacket(stream);

                if (message == null || message.msgid != (uint)global::MAVLink.MAVLINK_MSG_ID.COMMAND_ACK) continue;

                global::MAVLink.mavlink_command_ack_t ack = message.ToStructure<global::MAVLink.mavlink_command_ack_t>();

                bool thisMachine = (machine.MachineId.Value == message.sysid);
                bool thisCmdAck = (ack.command == (ushort)cmd);

                if (thisMachine && thisCmdAck)
                {
                    if (ack.result == (byte)global::MAVLink.MAV_RESULT.ACCEPTED)
                    {
                        return true;
                    }

                    if (ack.result == (byte)global::MAVLink.MAV_RESULT.IN_PROGRESS && !receivedInProgress)
                    {
                        HermesUtils.HermesLogInfo($"Command {cmd} for MachineID: {machine.MachineId} in progress, extending timeout from {MAX_NORMAL_WAIT_TIME_MS}ms to {IN_PROGRESS_WAIT_TIME_MS}ms");
                        future = DateTime.Now.Add(extendedTimeout);
                        receivedInProgress = true;
                    }
                    else
                    {
                        HermesUtils.HermesLogWarning($"Command {cmd} for MachineID: {machine.MachineId} returned result: {ack.result}");
                        return false;
                    }
                }
            }
        }

        if (receivedInProgress)
        {
            HermesUtils.HermesLogError($"Command {cmd} for MachineID: {machine.MachineId} timed out while in progress after {IN_PROGRESS_WAIT_TIME_MS}ms");
        }

        return false;
    }


}
