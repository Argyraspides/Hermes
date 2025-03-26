namespace Hermes.Core.Vehicle;

using Hermes.Core.Vehicle.States;

/// <summary>
/// A helper class that translates Hellenic messages into updates to vehicle state objects.
/// </summary>
public static class HellenicStateUpdater
{
    /// <summary>
    /// Updates the appropriate state objects based on the received Hellenic message
    /// </summary>
    public static void UpdateStates(
        HellenicMessage message,
        PositionState position,
        OrientationState orientation,
        VelocityState velocity,
        IdentityState identity)
    {
        switch (message.Id)
        {
            case (uint)HellenicMessageType.LatitudeLongitude:
                UpdatePositionFromLatLon(message as LatitudeLongitude, position);
                break;

            case (uint)HellenicMessageType.Altitude:
                UpdatePositionFromAltitude(message as Altitude, position);
                break;

            case (uint)HellenicMessageType.Heading:
                UpdateOrientationFromHeading(message as Heading, orientation);
                break;

            case (uint)HellenicMessageType.GroundVelocity:
                UpdateVelocityFromGroundVelocity(message as GroundVelocity, velocity);
                break;

            case (uint)HellenicMessageType.Pulse:
                UpdateIdentityFromPulse(message as Pulse, identity);
                break;
        }
    }

    private static void UpdatePositionFromLatLon(LatitudeLongitude latLon, PositionState position)
    {
        if (latLon == null) return;

        position.Latitude = latLon.Lat;
        position.Longitude = latLon.Lon;
        position.ReferenceFrame = latLon.ReferenceFrame;
        position.TimeUsec = latLon.TimeUsec;
    }

    private static void UpdatePositionFromAltitude(Altitude alt, PositionState position)
    {
        if (alt == null) return;

        position.Altitude = alt.Alt;
        position.RelativeAltitude = alt.RelativeAlt;
        // Don't overwrite the timestamp if this update is older than our position
        if (alt.TimeUsec > position.TimeUsec)
        {
            position.TimeUsec = alt.TimeUsec;
        }
        // position.AltitudeSource could be set if the message included source info
    }

    private static void UpdateOrientationFromHeading(Heading hdg, OrientationState orientation)
    {
        if (hdg == null) return;

        orientation.Heading = hdg.Hdg;
        orientation.ReferenceFrame = hdg.ReferenceFrame;
        orientation.TimeUsec = hdg.TimeUsec;
        // orientation.HeadingSource could be set if the message included source info
    }

    private static void UpdateVelocityFromGroundVelocity(GroundVelocity vel, VelocityState velocity)
    {
        if (vel == null) return;

        velocity.VelocityX = vel.Vx;
        velocity.VelocityY = vel.Vy;
        velocity.VelocityZ = vel.Vz;
        velocity.TimeUsec = vel.TimeUsec;
        // velocity.VelocitySource could be set if the message included source info
    }

    private static void UpdateIdentityFromPulse(Pulse pulse, IdentityState identity)
    {
        if (pulse == null) return;
        identity.VehicleId = pulse.EntityId;
        identity.VehicleType = (MachineType)pulse.VehicleType;
        identity.Callsign = pulse.Callsign;
        identity.TimeUsec = pulse.TimeUsec;
    }
}
