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
using System;
using Hermes.Common.Planet;
using Hermes.Common.Map.Utils;

// All planets are assumed to be describable by an ellipsoid.
public partial class PlanetOrbitalCamera : Camera3D
{
    [Signal]
    public delegate void OrbitalCameraLatLonChangedEventHandler(double latitude, double longitude);

    [Signal]
    public delegate void OrbitalCameraAltChangedEventHandler(double altitude);

    [Export] public PlanetShapeType PlanetType { get; set; } = PlanetShapeType.WGS84_ELLIPSOID;

    // Camera distance parameters - set based on planet type
    [Export] private double m_minCameraAltitude = 0.0d;
    [Export] private double m_maxCameraAltitude = 0.0d;
    [Export] private double m_initialCameraAltitude = 0.0d;

    // Multipliers for camera distances to ensure planet is in full view
    [Export] private double m_minAltitudeMultiplier = 1.0;
    [Export] private double m_maxAltitudeMultiplier = 10.0;
    [Export] private double m_initialAltitudeMultiplier = 3.0; // Good starting point for full planet view

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
    public double Lat
    { get { return m_currentLat; } }


    private double m_currentLon = 0.0d;
    public double Lon
    { get { return m_currentLon; } }

    public double m_currentAltitude = 10000.0d;
    public double CurrentAltitude { get { return m_currentAltitude; } }

    public int CurrentZoomLevel = 0;

    private double m_planetSemiMajorAxis;
    private double m_planetSemiMinorAxis;


    public override void _Ready()
    {
        InitializeExportedFields();
        SetPlanetParameters(PlanetType);

        m_currentAltitude = m_planetSemiMajorAxis * m_initialAltitudeMultiplier;
        m_currentLon = 0.0d;
        m_currentLat = 0.0d;

        PositionCamera();

        GetTree().Root.Ready += OnSceneTreeReady;
    }

    private void OnSceneTreeReady()
    {
        // Let everyone who is interested know our initial camera position
        // after everyone has loaded into the scene tree
        EmitSignal(SignalName.OrbitalCameraLatLonChanged, Lat, Lon);
        EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
    }

    public void InitializeExportedFields()
    {
        PlanetType = PlanetShapeType.WGS84_ELLIPSOID;

        m_minAltitudeMultiplier = 1.0;
        m_maxAltitudeMultiplier = 10.0;
        m_initialAltitudeMultiplier = 3.0;

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
        double targetLat = m_currentLat + dragVector.Y;
        targetLat = Math.Clamp(targetLat, -Math.PI / 2.0d, Math.PI / 2.0d);

        double northPoleThresh = (Math.PI / 2.0d) - m_poleThreshold;
        double southPoleThresh = -northPoleThresh;


        if (targetLat < northPoleThresh && targetLat > southPoleThresh)
        {
            m_currentLat = targetLat;
        }

        m_currentLon -= dragVector.X;
        m_currentLon = (m_currentLon < -Math.PI) ?  Math.PI : m_currentLon;
        m_currentLon = (m_currentLon >  Math.PI) ? -Math.PI : m_currentLon;

        PositionCamera();
        DeterminePanSpeed();

        EmitSignal(SignalName.OrbitalCameraLatLonChanged, Lat, Lon);
    }

    private void HandleCameraZooming(InputEventMouseButton mouseEvent)
    {
        bool u = mouseEvent.ButtonIndex == MouseButton.WheelUp;
        bool d = mouseEvent.ButtonIndex == MouseButton.WheelDown;
        if (u || d)
        {
            DetermineZoomSpeed();
            m_currentAltitude += u ? -m_cameraZoomSpeed : m_cameraZoomSpeed;
            PositionCamera();
            EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
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

        // TODO::ARGYRASPIDES() { Right now map utils assumes this function is talking about the earth }
        int latTile = MapUtils.LatitudeToTileCoordinateMercator(Lat, CurrentZoomLevel);
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
        if (CurrentZoomLevel == 0)
        {
            CurrentZoomLevel = 1;
        }

        int latTile = MapUtils.LatitudeToTileCoordinateMercator(Lat, CurrentZoomLevel);
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

    private void PositionCamera()
    {
        m_currentAltitude = Math.Clamp(m_currentAltitude, m_minCameraAltitude, m_maxCameraAltitude);
        Position = MapUtils.LatLonToCartesian(m_currentLat, m_currentLon, m_currentAltitude);
        LookAt(Vector3.Zero, Vector3.Up);
    }

    // Sets camera parameters based on the planet type
    public void SetPlanetParameters(PlanetShapeType planetType)
    {
        (double planetSemiMajorAxis, double planetSemiMinorAxis) = MapUtils.GetPlanetSemiMajorAxis(planetType);
        m_planetSemiMajorAxis = planetSemiMajorAxis;
        m_planetSemiMinorAxis = planetSemiMinorAxis;
        m_minCameraAltitude = 0.2d;
        m_maxCameraAltitude = planetSemiMajorAxis * m_maxAltitudeMultiplier;
        m_initialCameraAltitude = planetSemiMajorAxis * m_initialAltitudeMultiplier;
    }
}
