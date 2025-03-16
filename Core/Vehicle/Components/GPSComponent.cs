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
                break;
        }
    }
}
