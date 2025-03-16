using System;
using Hermes.Core.Vehicle.Components.ComponentStates;

namespace Hermes.Core.Vehicle.Components;

public class GPSComponent : Component
{
    public GPSComponentState m_gpsState;

    public GPSComponent()
    {
        ComponentType = ComponentType.GPS;
        m_gpsState = new GPSComponentState();
    }

    public override void UpdateComponentState(HellenicMessage message)
    {
        switch (message.Id)
        {
            case (uint)HellenicMessageType.LatitudeLongitude:
                LatitudeLongitude latlon = message as LatitudeLongitude;
                m_gpsState.Latitude = latlon.Lat;
                m_gpsState.Longitude = latlon.Lon;
                m_gpsState.TimeUsec = latlon.TimeUsec;
                m_gpsState.ReferenceFrame = latlon.ReferenceFrame;
                break;
            case (uint)HellenicMessageType.Altitude:
                Altitude alt = message as Altitude;
                m_gpsState.Altitude = alt.Alt;
                m_gpsState.TimeUsec = alt.TimeUsec;
                break;
            case (uint)HellenicMessageType.Heading:
                Heading hdg = message as Heading;
                m_gpsState.Heading = hdg.Hdg;
                m_gpsState.TimeUsec = hdg.TimeUsec;
                m_gpsState.ReferenceFrame = hdg.ReferenceFrame;
                break;
        }
    }
}
