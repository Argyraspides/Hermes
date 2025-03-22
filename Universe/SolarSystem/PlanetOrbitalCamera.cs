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
using Hermes.Common.Map.Utils;

namespace Hermes.Universe.SolarSystem;

using Godot;
using Hermes.Common.Planet;

// All planets are assumed to be describable by an ellipsoid.
public partial class PlanetOrbitalCamera : Camera3D
{
    [Signal]
    public delegate void OrbitalCameraLatLonChangedEventHandler(double latitude, double longitude);

    [Signal]
    public delegate void OrbitalCameraAltChangedEventHandler(double altitude);

    [Export] public PlanetShapeType PlanetType { get; set; } = PlanetShapeType.WGS84_ELLIPSOID;

    // Camera distance parameters - set based on planet type
    [Export] private double m_minCameraRadialDistance = 0.0d;
    [Export] private double m_maxCameraRadialDistance = 0.0d;
    [Export] private double m_initialCameraRadialDistance = 0.0d;

    // Multipliers for camera distances to ensure planet is in full view
    [Export] private double m_minDistanceMultiplier = 1.0;
    [Export] private double m_maxDistanceMultiplier = 10.0;
    [Export] private double m_initialDistanceMultiplier = 3.0; // Good starting point for full planet view

    // Current distances from planet center
    [Export] private double m_currentDistance = 0.0d;

    // Camera control settings
    [Export] private Vector2 m_cameraPanSpeedMultiplier = new Vector2(1,1);
    [Export] private Vector2 m_cameraPanSpeed = new Vector2(1,1);     // Speed of camera panning

    [Export] private double m_cameraZoomSpeed = 1.0;            // Speed of camera zooming
    [Export] private double m_cameraZoomSpeedMultiplier = 1.0;
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
        DetermineCameraAltitude();
        m_currentDistance = m_planetSemiMajorAxis * m_initialDistanceMultiplier;
    }

    public void InitializeExportedFields()
    {
        PlanetType = PlanetShapeType.WGS84_ELLIPSOID;

        m_minDistanceMultiplier = 1.0;
        m_maxDistanceMultiplier = 10.0;
        m_initialDistanceMultiplier = 3.0;

        m_cameraPanSpeedMultiplier = new Vector2(
            0.0175f,
            0.0175f
        );
        DeterminePanSpeed();

        m_cameraZoomSpeedMultiplier = new Vector2(
            5000.0f,
            5000.0f).Length();
        DetermineZoomSpeed();

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

        Vector2 dragVector = (dragEvent.ScreenRelative * m_cameraPanSpeed);

        // Prevent flipping the camera over the poles
        double targetLat = (m_currentLat + dragVector.Y) % Math.PI;
        if (targetLat < -m_poleThreshold && targetLat > (-Math.PI + m_poleThreshold))
        {
            m_currentLat = targetLat;
        }

        m_currentLon = (m_currentLon + dragVector.X) % (Math.PI * 2);

        PositionCamera();
        DeterminePanSpeed();

        EmitSignal(SignalName.OrbitalCameraLatLonChanged, DisplayLat, DisplayLon);
    }

    private void HandleCameraZooming(InputEventMouseButton mouseEvent)
    {
        // TODO::ARGYRASPIDES() { Right now map utils assumes this function is talking about the earth }
        if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
        {
            DetermineZoomSpeed();
            m_currentDistance -= m_cameraZoomSpeed;
            PositionCamera();
            EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
            DetermineCameraAltitude();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
        {
            DetermineZoomSpeed();
            m_currentDistance += m_cameraZoomSpeed;
            PositionCamera();
            EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
            DetermineCameraAltitude();
        }
    }

    private void DetermineZoomSpeed()
    {
        // TODO::ARGYRASPIDES() {  }
        // Find a cleaner way to deal with this. The terrain quad tree is technically the sole arbiter of zoom levels.
        // Perhaps the quadtree should own the camera? There can be no LoD if there is no observer for detail ...
        if (CurrentZoomLevel == 0)
        {
            CurrentZoomLevel = 1;
        }

        int latTile = MapUtils.LatitudeToTileCoordinateMercator(DisplayLat, CurrentZoomLevel);
        double latRange = MapUtils.TileToLatRange(latTile, CurrentZoomLevel);
        double lonRange = MapUtils.TileToLonRange(CurrentZoomLevel);

        m_cameraZoomSpeed = new Vector2(
            Mathf.Log(CurrentZoomLevel) * // ln(ZoomLevel) Approximates the curve of map tile longitude range decreasing with increasing zoom level
            (1.0f / CurrentZoomLevel) *   // Weighting bias for higher zoom levels -- the amount we zoom in by also decreases as we zoom in more
            (float)lonRange,
            Mathf.Log(CurrentZoomLevel) *
            (1.0f / CurrentZoomLevel) *
            (float)latRange
        ).Length() * m_cameraZoomSpeedMultiplier;
    }

    private void DeterminePanSpeed()
    {
        int latTile = MapUtils.LatitudeToTileCoordinateMercator(DisplayLat, CurrentZoomLevel);
        double latRange = MapUtils.TileToLatRange(latTile, CurrentZoomLevel);
        double lonRange = MapUtils.TileToLonRange(CurrentZoomLevel);

        m_cameraPanSpeed = new Vector2(
            Mathf.Log(CurrentZoomLevel) *  // ln(ZoomLevel) Approximates the curve of map tile longitude range decreasing with increasing zoom level
            (1.0f / CurrentZoomLevel) *    // Weighting bias for higher zoom levels -- the amount we pan by also decreases as we zoom in more
            (float)lonRange,
            Mathf.Log(CurrentZoomLevel) *
            (1.0f / CurrentZoomLevel) *
            (float)latRange
            ) * m_cameraPanSpeedMultiplier;
    }

    private void DetermineCameraAltitude()
    {
        // TODO::ARGYRASPIDES() { Validate that this is the actual, true altitude above the current point on the planet's surface.
        // The numbers appear correct but they're ever so slightly off from Google Earth's measurements, though I can't be sure

        // Point on the planets surface above our current lat/lon point
        // TODO::ARGYRASPIDES() { The map utils function below assumes the dimensions of the earth. Change soon! }
        Vector3 surfacePoint = MapUtils.LatLonToCartesian(m_currentLat, m_currentLon);
        CurrentAltitude = surfacePoint.DistanceTo(Position);
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
        (double planetSemiMajorAxis, double planetSemiMinorAxis) = MapUtils.GetPlanetSemiMajorAxis(planetType);
        m_planetSemiMajorAxis = planetSemiMajorAxis;
        m_planetSemiMinorAxis = planetSemiMinorAxis;
        m_minCameraRadialDistance = planetSemiMajorAxis * m_minDistanceMultiplier;
        m_maxCameraRadialDistance = planetSemiMajorAxis * m_maxDistanceMultiplier;
        m_initialCameraRadialDistance = planetSemiMajorAxis * m_initialDistanceMultiplier;
    }

    public void ChangePlanet(PlanetShapeType planetType)
    {
        PlanetType = planetType;
        SetPlanetParameters(planetType);

        // Reset the camera position
        m_currentDistance = m_initialCameraRadialDistance;
    }
}
