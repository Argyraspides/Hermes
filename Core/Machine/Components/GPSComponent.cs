using System;
using System.ComponentModel;
using Hermes.Core.Machine.Components.ComponentStates;

namespace Hermes.Core.Machine.Components;

public class GPSComponent : Component
{
    public GPSComponentState GPSState { get; private set; }

    public GPSComponent()
    {
        ComponentType = ComponentType.GPS;
        GPSState = new GPSComponentState();
    }

    public override void UpdateComponentState(HellenicMessage message)
    {
        switch (message.Id)
        {
            case (uint)HellenicMessageType.LatitudeLongitude:
                LatitudeLongitude latlon = message as LatitudeLongitude;
                GPSState.Latitude = latlon.Lat;
                GPSState.Longitude = latlon.Lon;
                GPSState.TimeUsec = latlon.TimeUsec;
                GPSState.ReferenceFrame = latlon.ReferenceFrame;
                break;
            case (uint)HellenicMessageType.Altitude:
                Altitude alt = message as Altitude;
                GPSState.Altitude = alt.Alt;
                GPSState.TimeUsec = alt.TimeUsec;
                break;
            case (uint)HellenicMessageType.Heading:
                Heading hdg = message as Heading;
                GPSState.Heading = hdg.Hdg;
                GPSState.TimeUsec = hdg.TimeUsec;
                GPSState.ReferenceFrame = hdg.ReferenceFrame;
                break;
        }
    }
}
