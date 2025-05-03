using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Hermes.Common.HermesUtils;
using Hermes.Common.Networking.UDP;
using Hermes.Common.Types;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

/*
 * TODO::ARGYRASPIDES() {
 *      there is some duplicate code here (awaiting for ack with retries has repetitive logic
 *      in each of the send mavlink command functions).
 *      fix that up please.
 *  }
 *
 */
public class MAVLinkCommander : IDisposable
{
    private const double LAT_LON_SCALE_FACTOR = 1e7;
    private const uint FORCE_ARM_VALUE = 21196;
    private const uint MAX_NORMAL_WAIT_TIME_MS = 1500;
    private const uint IN_PROGRESS_WAIT_TIME_MS = 3000;
    private const uint MAX_RETRIES = 1;

    private byte GCS_MAVLINK_ID = 255;

    private int MAVLINK_UDP_RECIEVE_PORT = 14445;
    private int MAVLINK_UDP_DST_CMD_PORT = 14580;
    private int HERMES_UDP_SRC_PORT = 11777;

    private global::MAVLink.MavlinkParse mavlinkParser = new global::MAVLink.MavlinkParse();

    // TODO::ARGYRASPIDES() { Figure out how you're gonna manage different links coz its not always gonna be UDP lol }
    private UdpClient sender;

    public MAVLinkCommander()
    {
        sender = new UdpClient(HERMES_UDP_SRC_PORT);
    }

    ~MAVLinkCommander()
    {
        Dispose(false);
    }

    public void SendMAVLinkTakeoffCommand(Machine machine, double altitude, double pitch = 0.0d, double yaw = 0.0d,
        Action<bool> successCallback = null)
    {
        if (machine == null)
        {
            HermesUtils.HermesLogError($"Cannot send takeoff command -- null vehicle.");
            return;
        }

        HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.LatitudeLongitude);

        if (msg == null || msg is not LatitudeLongitude latlon)
        {
            HermesUtils.HermesLogError(
                $"Cannot send takeoff command -- vehicle lat/lon unknown. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
            return;
        }

        if (!latlon.Lat.HasValue || !latlon.Lon.HasValue)
        {
            HermesUtils.HermesLogError(
                $"Cannot send takeoff command -- vehicle lat/lon unknown. MachineID: {machine.MachineId}");
            return;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot send takeoff command -- vehicle machineId unknown");
            return;
        }

        int lat = (int)(latlon.Lat.Value * LAT_LON_SCALE_FACTOR);
        int lon = (int)(latlon.Lon.Value * LAT_LON_SCALE_FACTOR);

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

        int attempts = 0;
        byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
            global::MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
            mavlinkCommand,
            false,
            GCS_MAVLINK_ID,
            (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
            attempts
        );

        Action<bool> ackAction = null;
        ackAction = (success) =>
        {
            if (success)
            {
                HermesUtils.HermesLogSuccess(
                    $"Successfully performed TAKEOFF command for machine #{machine.MachineId}");
                successCallback?.Invoke(true);
            }
            else if (attempts < MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink TAKEOFF command after {attempts} attempts. MachineID: {machine.MachineId}, altitude: {altitude}. Retrying ...");
                attempts++;
                byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                    global::MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
                    mavlinkCommand,
                    false,
                    GCS_MAVLINK_ID,
                    (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                    attempts
                );
                sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
                AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.TAKEOFF, ackAction);
            }
            else if (attempts >= MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink COMPONENT_ARM_DISARM command after the max number of attempts. MachineID: {machine.MachineId}, altitude: {altitude}. Aborting ...");
                successCallback?.Invoke(false);
            }
        };

        sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
        AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.TAKEOFF, ackAction);
    }

    public void SendMAVLinkArmCommand(Machine machine, bool forceArm = false, Action<bool> successCallback = null)
    {
        if (machine == null || !machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError(
                $"Cannot send ARM command to null vehicle/vehicle without an ID. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
            return;
        }

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

        int attempts = 0;
        byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
            global::MAVLink.MAVLINK_MSG_ID.COMMAND_LONG,
            mavlinkCommand,
            false,
            GCS_MAVLINK_ID,
            (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
            attempts
        );

        Action<bool> ackAction = null;
        ackAction = (success) =>
        {
            if (success)
            {
                HermesUtils.HermesLogSuccess(
                    $"Successfully performed ARM command for machine #{machine.MachineId}");
                successCallback?.Invoke(true);
            }
            else if (attempts < MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink COMPONENT_ARM_DISARM command after {attempts} attempts. MachineID: {machine.MachineId}, ForceArm: {forceArm}. Retrying ...");
                attempts++;
                byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                    global::MAVLink.MAVLINK_MSG_ID.COMMAND_LONG,
                    mavlinkCommand,
                    false,
                    GCS_MAVLINK_ID,
                    (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                    attempts
                );
                sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
                AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.COMPONENT_ARM_DISARM, ackAction);
            }
            else if (attempts >= MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink COMPONENT_ARM_DISARM command after the max number of attempts. MachineID: {machine.MachineId}, ForceArm: {forceArm}. Aborting ...");
                successCallback?.Invoke(false);
            }
        };

        sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
        AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.COMPONENT_ARM_DISARM, ackAction);
    }

    public void SendMAVLinkLandCommand(
        Machine machine,
        double abortAlt = 0.0d,
        global::MAVLink.PRECISION_LAND_MODE landmode = 0,
        double yaw = 0.0d,
        double altitude = 0.0d,
        Action<bool> successCallback = null)
    {
        HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.LatitudeLongitude);
        if (msg == null || msg is not LatitudeLongitude latlon)
        {
            HermesUtils.HermesLogError(
                $"Cannot send land command -- vehicle lat/lon unknown. MachineID: {(machine?.MachineId?.ToString() ?? "null")}");
            return;
        }

        if (!latlon.Lat.HasValue || !latlon.Lon.HasValue)
        {
            HermesUtils.HermesLogError(
                $"Cannot send land command -- vehicle lat/lon unknown. MachineID: {machine.MachineId}");
            return;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot send land command -- vehicle machineId unknown");
            return;
        }

        int lat = (int)(latlon.Lat.Value * LAT_LON_SCALE_FACTOR);
        int lon = (int)(latlon.Lon.Value * LAT_LON_SCALE_FACTOR);

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

        int attempts = 0;
        byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
            global::MAVLink.MAVLINK_MSG_ID.COMMAND_INT,
            mavlinkCommand,
            false,
            GCS_MAVLINK_ID,
            (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
            attempts
        );

        Action<bool> ackAction = null;
        ackAction = (success) =>
        {
            if (success)
            {
                HermesUtils.HermesLogSuccess(
                    $"Successfully performed LAND command for machine #{machine.MachineId}");
                successCallback?.Invoke(true);
            }
            else if (attempts < MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink LAND command after {attempts} attempts. MachineID: {machine.MachineId}. Retrying ...");
                attempts++;
                byte[] packet = mavlinkParser.GenerateMAVLinkPacket20(
                    global::MAVLink.MAVLINK_MSG_ID.COMMAND_LONG,
                    mavlinkCommand,
                    false,
                    GCS_MAVLINK_ID,
                    (byte)global::MAVLink.MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER,
                    attempts
                );
                sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
                AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.LAND, ackAction);
            }
            else if (attempts >= MAX_RETRIES)
            {
                HermesUtils.HermesLogWarning(
                    $"Unable to send MAVLink LAND command after the max number of attempts. MachineID: {machine.MachineId}. Aborting ...");
                successCallback?.Invoke(false);
            }
        };

        sender.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, MAVLINK_UDP_DST_CMD_PORT));
        AwaitMAVLinkAcknowledgement(machine, global::MAVLink.MAV_CMD.LAND, ackAction);
    }

    public void AwaitMAVLinkAcknowledgement(Machine machine, global::MAVLink.MAV_CMD cmd, Action<bool> ackCallback)
    {
        if (machine == null)
        {
            HermesUtils.HermesLogError($"Cannot await for an acknowledgement of a null vehicle.");
            return;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot wait for an acknowledgement of a vehicle without an ID");
            return;
        }

        ConcurrentBoolean receivedInProgress = new ConcurrentBoolean(false);
        ConcurrentBoolean receivedInProgressSet = new ConcurrentBoolean(false);
        ConcurrentBoolean commandSuccess = new ConcurrentBoolean(false);

        var initialTimeout = TimeSpan.FromMilliseconds(MAX_NORMAL_WAIT_TIME_MS);
        var extendedTimeout = TimeSpan.FromMilliseconds(IN_PROGRESS_WAIT_TIME_MS);

        var future = DateTime.Now.Add(initialTimeout);

        Action<string> listenForAck = (subKey) =>
        {
            // We've already won
            if (commandSuccess.Value)
            {
                return;
            }

            // Exceeded timeout with no in-progress update, or exceeded timeout even after
            // in-progress extension
            if ((DateTime.Now > future && !receivedInProgress.Value) ||
                (DateTime.Now > future && receivedInProgressSet.Value))
            {
                HermesUDPListener.DeregisterUdpClient(subKey);
                ackCallback?.Invoke(false);
            }

            // Exceeded timeout but machine tells us it's till in progress -- give it some more time
            if (DateTime.Now > future && receivedInProgress.Value)
            {
                future = DateTime.Now.Add(extendedTimeout);
                receivedInProgressSet.Set(true);
            }

            var dat = HermesUDPListener.Receive(subKey);
            if (dat == null || dat.Buffer == null || dat.Buffer.Length == 0) return;

            using (MemoryStream stream = new MemoryStream(dat.Buffer))
            {
                global::MAVLink.MAVLinkMessage message = mavlinkParser.ReadPacket(stream);

                if (message == null || message.msgid != (uint)global::MAVLink.MAVLINK_MSG_ID.COMMAND_ACK) return;

                global::MAVLink.mavlink_command_ack_t
                    ack = message.ToStructure<global::MAVLink.mavlink_command_ack_t>();

                bool thisMachine = (machine.MachineId.Value == message.sysid);
                bool thisCmdAck = (ack.command == (ushort)cmd);

                if (thisMachine && thisCmdAck)
                {
                    if (ack.result == (byte)global::MAVLink.MAV_RESULT.ACCEPTED)
                    {
                        HermesUDPListener.DeregisterUdpClient(subKey);
                        commandSuccess.Set(true);
                        ackCallback?.Invoke(true);
                    }
                    else if (ack.result == (byte)global::MAVLink.MAV_RESULT.IN_PROGRESS && !receivedInProgress.Value)
                    {
                        receivedInProgress.Set(true);
                    }
                    else
                    {
                        HermesUtils.HermesLogWarning(
                            $"Command {cmd} for MachineID: {machine.MachineId} returned result: {ack.result}");
                    }
                }
            }
        };

        IPEndPoint receiverEndPoint = IPEndPoint.Parse($"127.0.0.1:{MAVLINK_UDP_RECIEVE_PORT}");
        string subKey = HermesUDPListener.RegisterUdpClient(receiverEndPoint, listenForAck);
        listenForAck.Invoke(subKey);
    }

    private void Dispose(bool manualDispose)
    {
        if (manualDispose)
        {
            sender?.Dispose();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }
}
