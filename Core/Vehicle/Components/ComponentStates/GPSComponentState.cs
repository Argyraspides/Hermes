namespace Hermes.Core.Vehicle.Components.ComponentStates;

public class GPSComponentState : ComponentState
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
    public double TimeUsec;

    // Reference frame (0 = Mercury, 1 = Venus, 2 = Earth, 3 = Mars ...)
    // Earth by default
    // TODO::ARGYRASPIDES() { Give reference frame an enum }
    public byte ReferenceFrame = 2;

    public GPSComponentState()
    {
        Latitude = double.NaN;
        Longitude = double.NaN;
        Altitude = double.NaN;
        Heading = double.NaN;
        GroundSpeedX = double.NaN;
        GroundSpeedY = double.NaN;
        GroundSpeedZ = double.NaN;
        ReferenceFrame = 2;
    }
}
