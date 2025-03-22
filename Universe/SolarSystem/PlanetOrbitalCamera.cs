/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/

using System;

namespace Hermes.Universe.SolarSystem;

using Godot;
using Hermes.Common.Planet;

public partial class PlanetOrbitalCamera : Camera3D
{
    [Signal]
    public delegate void OrbitalCameraLatLonChangedEventHandler(double latitude, double longitude);

    [Signal]
    public delegate void OrbitalCameraAltChangedEventHandler(double altitude);

    [Export] public PlanetShapeType PlanetType { get; set; } = PlanetShapeType.WGS84_ELLIPSOID;

    // Camera distance parameters - set based on planet type
    [Export] private double m_minCameraRadialDistance;
    [Export] private double m_maxCameraRadialDistance;
    [Export] private double m_initialCameraRadialDistance;

    // Multipliers for camera distances to ensure planet is in full view
    [Export] private double m_minDistanceMultiplier = 1.0;
    [Export] private double m_maxDistanceMultiplier = 10.0;
    [Export] private double m_initialDistanceMultiplier = 3.0; // Good starting point for full planet view

    // Current distances from planet center
    [Export] private double m_currentDistance;

    // Camera control settings
    [Export] private double m_cameraPanSpeed = 7d;     // Speed of camera panning
    [Export] private double m_cameraZoomSpeed = 1.0;   // Speed of camera zooming
    [Export] private double m_poleThreshold = 0.15d;   // Degrees of latitude from the poles (radians) to lock the camera

    // Latitude and longitude that the center of the camera is looking at (radians)
    // DisplayLat and DisplayLon are offset to show the user the lat/lon position
    // in a standard format
    private double m_currentLat = 0.0d;
    public double DisplayLat
    {
        get
        {
            return m_currentLat + (Math.PI / 2.0);
        }
    }

    public double TrueLat
    {
        get
        {
            return m_currentLat;
        }
    }


    private double m_currentLon = 0.0d;
    public double DisplayLon
    {
        get
        {
            return -m_currentLon;
        }
    }
    public double TrueLon
    {
        get
        {
            return m_currentLon;
        }
    }

    public double CurrentAltitude { get; set; }

    public int CurrentZoomLevel = 0;

    private double m_planetSemiMajorAxis;
    private double m_planetSemiMinorAxis;


    public override void _Ready()
    {
        InitializeExportedFields();
        SetPlanetParameters(PlanetType);

        m_currentDistance = m_planetSemiMajorAxis * m_initialDistanceMultiplier;
    }

    public void InitializeExportedFields()
    {
        PlanetType = PlanetShapeType.WGS84_ELLIPSOID;

        m_minDistanceMultiplier = 1.0;
        m_maxDistanceMultiplier = 10.0;
        m_initialDistanceMultiplier = 3.0;

        m_cameraPanSpeed = 0.01d;
        m_poleThreshold = 0.15d;
        m_currentLat = 0.0d;
        m_currentLon = 0.0d;
    }

    public override void _Process(double delta)
    {

    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenDrag dragEvent)
        {
            HandleCameraPanning(dragEvent);
        }

        if (@event is InputEventMouseButton mouseEvent)
        {
            HandleCameraZooming(mouseEvent);
        }
    }

    // Ensure you have "Emulate Touch From Mouse" enabled in Godot.
    // Settings >> General >> Input Devices on the side tab >> Pointing (or just search for it bruh)
    private void HandleCameraPanning(InputEventScreenDrag dragEvent)
    {
        // X = longitude, Y = latitude

        Vector2 dragVector = dragEvent.ScreenRelative * (float)m_cameraPanSpeed;

        // Prevent flipping the camera over the poles
        double targetLat = (m_currentLat + dragVector.Y) % Math.PI;
        if (targetLat < -m_poleThreshold && targetLat > (-Math.PI + m_poleThreshold))
        {
            m_currentLat = targetLat;
        }

        m_currentLon = (m_currentLon + dragVector.X) % (Math.PI * 2);

        PositionCamera();

        EmitSignal(SignalName.OrbitalCameraLatLonChanged, DisplayLat, DisplayLon);
    }

    private void HandleCameraZooming(InputEventMouseButton mouseEvent)
    {
        m_cameraZoomSpeed = 1;
        if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
        {
            m_currentDistance -= m_cameraZoomSpeed;
            PositionCamera();
            EmitSignal(SignalName.OrbitalCameraAltChanged, m_currentDistance);
            GD.Print("Current distance in cam: " + m_currentDistance);

        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
        {
            m_currentDistance += m_cameraZoomSpeed;
            PositionCamera();
            EmitSignal(SignalName.OrbitalCameraAltChanged, m_currentDistance);
        }
    }

    private void DetermineZoomSpeed()
    {

    }

    private void DeterminePanSpeed()
    {

    }

    private void PositionCamera()
    {
        // Points on ellipsoid (remember Godot has 'y' as up and 'z' perpendicular to 'x', so the 'y' and 'z' equations are swapped here)
        // 'a', 'b', and 'c' are semi-major/minor axes. In our case we always orient the planets so that their axis of rotation is
        // on the +ve y-axis, 'a' and 'b' are therefore the semi-major axes, and 'c' is the semi-minor axis
        // x = a cos(u) sin(v)
        // y = c cos(v)
        // z = b sin(u) sin(v)

        Position = new Vector3(
            (float)(m_currentDistance * Math.Cos(m_currentLon) * Math.Sin(m_currentLat)),
            (float)(m_currentDistance * Math.Cos(m_currentLat)),
            (float)(m_currentDistance * Math.Sin(m_currentLon) * Math.Sin(m_currentLat))
        );

        LookAt(Vector3.Zero, Vector3.Up);

    }

    public void InitializeCameraPosition(Vector3 position)
    {
        Position = position;
        PositionCamera();
    }

    // Sets camera parameters based on the planet type
    public void SetPlanetParameters(PlanetShapeType planetType)
    {
        (double planetSemiMajorAxis, double planetSemiMinorAxis) = GetPlanetSemiMajorAxis(planetType);
        m_planetSemiMajorAxis = planetSemiMajorAxis;
        m_planetSemiMinorAxis = planetSemiMinorAxis;
        m_minCameraRadialDistance = planetSemiMajorAxis * m_minDistanceMultiplier;
        m_maxCameraRadialDistance = planetSemiMajorAxis * m_maxDistanceMultiplier;
        m_initialCameraRadialDistance = planetSemiMajorAxis * m_initialDistanceMultiplier;
    }

    private (double, double) GetPlanetSemiMajorAxis(PlanetShapeType planetType)
    {
        switch (planetType)
        {
            case PlanetShapeType.SPHERE:
                return (SolarSystemConstants.BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.BLANK_PLANET_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.WGS84_ELLIPSOID:
                return (SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.MERCURY:
                return (SolarSystemConstants.MERCURY_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.MERCURY_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.VENUS:
                return (SolarSystemConstants.VENUS_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.VENUS_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.MARS:
                return (SolarSystemConstants.MARS_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.MARS_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.JUPITER:
                return (SolarSystemConstants.JUPITER_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.JUPITER_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.SATURN:
                return (SolarSystemConstants.SATURN_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.SATURN_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.URANUS:
                return (SolarSystemConstants.URANUS_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.URANUS_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.NEPTUNE:
                return (SolarSystemConstants.NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.NEPTUNE_SEMI_MINOR_AXIS_LEN_KM);

            case PlanetShapeType.UNKNOWN:
                return (SolarSystemConstants.BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.BLANK_PLANET_SEMI_MINOR_AXIS_LEN_KM);
            default:
                return (SolarSystemConstants.BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_KM, SolarSystemConstants.BLANK_PLANET_SEMI_MINOR_AXIS_LEN_KM);

        }
    }

    public void ChangePlanet(PlanetShapeType planetType)
    {
        PlanetType = planetType;
        SetPlanetParameters(planetType);

        // Reset the camera position
        m_currentDistance = m_initialCameraRadialDistance;
    }
}
