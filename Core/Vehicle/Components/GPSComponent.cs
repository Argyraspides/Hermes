namespace Hermes.Core.Vehicle.Components;

public struct GPSComponent
{
    // Supported by pretty much all GPS components out there
    public double Latitude;
    public double Longitude;
    public double Altitude;

    // Supported by most GPS components out there
    public double Heading;
    public double GroundSpeedX;
    public double GroundSpeedY;
    public double GroundSpeedZ;

    public GPSComponent()
    {
        Latitude = double.NaN;
        Longitude = double.NaN;
        Altitude = double.NaN;
        Heading = double.NaN;
        GroundSpeedX = double.NaN;
        GroundSpeedY = double.NaN;
        GroundSpeedZ = double.NaN;
    }
}
