using Godot;
using Hermes.Common.Map.Utils;

namespace Hermes.Core.Machine.States;

/// <summary>
/// A helper class that translates Hellenic messages into updates to machine state objects.
/// </summary>
public static class HellenicStateUpdater
{
    /// <summary>
    /// Updates the appropriate state objects based on the received Hellenic message
    /// </summary>
    public static void UpdateStates(
        HellenicMessage message,
        Machine machine)
    {
        switch (message.Id)
        {
            case (uint)HellenicMessageType.LatitudeLongitude:
                UpdatePositionFromLatLon(message as LatitudeLongitude, machine);
                break;

            case (uint)HellenicMessageType.Altitude:
                UpdatePositionFromAltitude(message as Altitude, machine);
                break;

            case (uint)HellenicMessageType.Heading:
                UpdateOrientationFromHeading(message as Heading, machine);
                break;

            case (uint)HellenicMessageType.GroundVelocity:
                UpdateVelocityFromGroundVelocity(message as GroundVelocity, machine);
                break;

            case (uint)HellenicMessageType.Pulse:
                UpdateIdentityFromPulse(message as Pulse, machine);
                break;
        }
    }

    private static void UpdatePositionFromLatLon(LatitudeLongitude latLon, Machine machine)
    {
        if (latLon == null) return;

        machine._Position.Latitude = latLon.Lat;
        machine._Position.Longitude = latLon.Lon;
        machine._Position.ReferenceFrame = latLon.ReferenceFrame;
        machine._Position.TimeUsec = latLon.TimeUsec;

        // TODO::ARGYRASPIDES() !URGENT! { This conversion is all fucked up. Fix it.
        // We are pretty close to the right position but not quite. }
        Vector3 globalPosition = MapUtils.LatLonToCartesian(
            Mathf.DegToRad(latLon.Lat),
            Mathf.DegToRad(latLon.Lon)
        );
        globalPosition.X *= -1;
        machine.GlobalPosition = globalPosition;

    }

    private static void UpdatePositionFromAltitude(Altitude alt, Machine machine)
    {
        if (alt == null) return;

        machine._Position.Altitude = alt.Alt;
        machine._Position.RelativeAltitude = alt.RelativeAlt;
        // Don't overwrite the timestamp if this update is older than our position
        if (alt.TimeUsec >  machine._Position.TimeUsec)
        {
            machine._Position.TimeUsec = alt.TimeUsec;
        }
        // position.AltitudeSource could be set if the message included source info
    }

    private static void UpdateOrientationFromHeading(Heading hdg, Machine machine)
    {
        if (hdg == null) return;

        machine.Orientation.Heading = hdg.Hdg;
        machine.Orientation.ReferenceFrame = hdg.ReferenceFrame;
        machine.Orientation.TimeUsec = hdg.TimeUsec;
        // orientation.HeadingSource could be set if the message included source info
    }

    private static void UpdateVelocityFromGroundVelocity(GroundVelocity vel, Machine machine)
    {
        if (vel == null) return;

        machine.Velocity.VelocityX = vel.Vx;
        machine.Velocity.VelocityY = vel.Vy;
        machine.Velocity.VelocityZ = vel.Vz;
        machine.Velocity.TimeUsec = vel.TimeUsec;
        // velocity.VelocitySource could be set if the message included source info
    }

    private static void UpdateIdentityFromPulse(Pulse pulse, Machine machine)
    {
        if (pulse == null) return;
        machine.Identity.MachineId = pulse.MachineId;
        machine.Identity.MachineType = (MachineType)pulse.MachineType;
        machine.Identity.Callsign = pulse.Callsign;
        machine.Identity.TimeUsec = pulse.TimeUsec;
    }
}
