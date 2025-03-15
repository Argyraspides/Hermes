using System;

namespace Hermes.Core.Vehicle.Components;

public class GPSComponent : Component
{
    // Supported by pretty much all GPS components out there
    public double Latitude = double.NaN;
    public double Longitude = double.NaN;
    public double Altitude = double.NaN;

    // Supported by most GPS components out there
    public double Heading = double.NaN;
    public double GroundSpeedX = double.NaN;
    public double GroundSpeedY = double.NaN;
    public double GroundSpeedZ = double.NaN;

    public GPSComponent()
    {
        ComponentType = ComponentType.GPS;
    }
}
