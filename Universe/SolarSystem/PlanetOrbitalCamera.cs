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
    private double m_targetDistance;

    // Latitude and longitude that the center of the camera is looking at
    [Export] public double CurrentLat { get; private set; }
    [Export] public double CurrentLon { get; private set; }
    [Export] public double CurrentAltitude { get; private set; }

    public override void _Ready()
    {
        // Initialize camera parameters based on planet type
        SetPlanetParameters(PlanetType);

        // Initialize distance
        m_currentDistance = m_initialCameraRadialDistance;
        m_targetDistance = m_initialCameraRadialDistance;
    }

    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
    }

    void UpdateVisibleLatLonRange()
    {
    }

    public void InitializeCameraPosition(Vector3 position)
    {
        Position = position;
        LookAt(Vector3.Zero, Vector3.Up);
    }

    // Sets camera parameters based on the planet type
    public void SetPlanetParameters(PlanetShapeType planetType)
    {
        // Get the planet's semi-major axis
        double planetRadius = GetPlanetSemiMajorAxis(planetType);

        m_minCameraRadialDistance = planetRadius * m_minDistanceMultiplier;
        m_maxCameraRadialDistance = planetRadius * m_maxDistanceMultiplier;
        m_initialCameraRadialDistance = planetRadius * m_initialDistanceMultiplier;
    }

    private double GetPlanetSemiMajorAxis(PlanetShapeType planetType)
    {
        switch (planetType)
        {
            case PlanetShapeType.SPHERE:
                return 6_000; // Generic value

            case PlanetShapeType.WGS84_ELLIPSOID:
                return SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.MERCURY:
                return SolarSystemConstants.MERCURY_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.VENUS:
                return SolarSystemConstants.VENUS_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.MARS:
                return SolarSystemConstants.MARS_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.JUPITER:
                return SolarSystemConstants.JUPITER_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.SATURN:
                return SolarSystemConstants.SATURN_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.URANUS:
                return SolarSystemConstants.URANUS_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.NEPTUNE:
                return SolarSystemConstants.NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM;

            case PlanetShapeType.UNKNOWN:
            default:
                return SolarSystemConstants.BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_KM;
        }
    }

    public void ChangePlanet(PlanetShapeType planetType)
    {
        PlanetType = planetType;
        SetPlanetParameters(planetType);

        // Reset the camera position
        m_currentDistance = m_initialCameraRadialDistance;
        m_targetDistance = m_initialCameraRadialDistance;
    }
}
