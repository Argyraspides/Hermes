/*

                              db         88888888ba     ,ad8888ba,   88        88   ad88888ba
                             d88b        88      "8b   d8"'    `"8b  88        88  d8"     "8b
                            d8'`8b       88      ,8P  d8'            88        88  Y8,
                           d8'  `8b      88aaaaaa8P'  88             88        88  `Y8aaaaa,
                          d8YaaaaY8b     88""""88'    88      88888  88        88    `"""""8b,
                         d8""""""""8b    88    `8b    Y8,        88  88        88          `8b
                        d8'        `8b   88     `8b    Y8a.    .a88  Y8a.    .a8P  Y8a     a8P
                       d8'          `8b  88      `8b    `"Y88888P"    `"Y8888Y"'    "Y88888P"



   88888888ba          db         888b      88    ,ad8888ba,    88888888ba   888888888888  88888888888   ad88888ba
   88      "8b        d88b        8888b     88   d8"'    `"8b   88      "8b       88       88           d8"     "8b
   88      ,8P       d8'`8b       88 `8b    88  d8'        `8b  88      ,8P       88       88           Y8,
   88aaaaaa8P'      d8'  `8b      88  `8b   88  88          88  88aaaaaa8P'       88       88aaaaa      `Y8aaaaa,
   88""""""'       d8YaaaaY8b     88   `8b  88  88          88  88""""""'         88       88"""""        `"""""8b,
   88             d8""""""""8b    88    `8b 88  Y8,        ,8P  88                88       88                   `8b
   88            d8'        `8b   88     `8888   Y8a.    .a8P   88                88       88           Y8a     a8P
   88           d8'          `8b  88      `888    `"Y8888Y"'    88                88       88888888888   "Y88888P"

                                                WATCHMAN OF THE WORLDS

*/

using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Universe.Autoloads.EventBus;

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
    [Export] private double m_maxAltitudeMultiplier = 2.25;
    [Export] private double m_initialAltitudeMultiplier = 2.0; // Good starting point for full planet view

    // Camera control settings
    [Export] private Vector2 m_cameraPanSpeedMultiplier = new Vector2(1, 1);
    [Export] private Vector2 m_cameraPanSpeed = new Vector2(1, 1);     // Speed of camera panning
    [Export] private Vector2 m_cameraPanSmoothing = new Vector2(0.1f, 0.1f);

    [Export] private double m_cameraZoomSpeed = 1.0;            // Speed of camera zooming
    [Export] private double m_cameraZoomSmoothing = 0.25;
    [Export] private double m_cameraZoomSpeedMultiplier = 1.0;
    [Export] private double m_poleThreshold = 0.15d;   // Degrees of latitude from the poles (radians) to lock the camera

    // Latitude and longitude that the center of the camera is looking at (radians)
    // DisplayLat and DisplayLon are offset to show the user the lat/lon position
    // in a standard format
    private double m_currentLat = 0.0d;
    private double m_targetLat = 0.0d;
    public double Lat
    { get { return m_currentLat; } }


    private double m_currentLon = 0.0d;
    private double m_targetLon = 0.0d;
    public double Lon
    { get { return m_currentLon; } }

    private double m_currentAltitude = 10000.0d;
    private double m_targetAltitude = 10000.0d;
    public double CurrentAltitude { get { return m_currentAltitude; } }

    public int CurrentZoomLevel = 0;

    private double m_planetSemiMajorAxis;
    private double m_planetSemiMinorAxis;


    public override void _Ready()
    {
        InitializeExportedFields();
        SetPlanetParameters(PlanetType);

        m_currentAltitude = m_planetSemiMajorAxis * m_initialAltitudeMultiplier;
        m_targetAltitude = m_currentAltitude;
        m_currentLon = 0.0d;
        m_currentLat = 0.0d;

        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;
        GlobalEventBus.Instance.UIEventBus.ZoomInButtonClicked += OnZoomInButtonClicked;
        GlobalEventBus.Instance.UIEventBus.ZoomOutButtonClicked += OnZoomOutButtonClicked;

        OrbitalCameraAltChanged += GlobalEventBus.Instance.PlanetaryEventBus.OnPlanetOrbitalCameraAltChanged;
        OrbitalCameraLatLonChanged += GlobalEventBus.Instance.PlanetaryEventBus.OnPlanetOrbitalCameraLatLonChanged;

        GetTree().Root.Ready += OnSceneTreeReady;
    }

    public override void _Process(double delta)
    {
        bool isZooming = m_targetAltitude != m_currentAltitude;
        bool isPanning = (m_targetLat != m_currentLat) || (m_targetLon != m_currentLon);
        if (!isZooming && !isPanning) return;
        PositionCamera();
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

    public override void _UnhandledInput(InputEvent @event)
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
            m_targetLat = targetLat;
        }

        m_targetLon -= dragVector.X;

        DeterminePanSpeed();
    }

    private void HandleCameraZooming(InputEventMouseButton mouseEvent)
    {
        bool u = mouseEvent.ButtonIndex == MouseButton.WheelUp;
        bool d = mouseEvent.ButtonIndex == MouseButton.WheelDown;
        if (!u && !d) return;

        DetermineZoomSpeed();
        m_targetAltitude += u ? -m_cameraZoomSpeed : m_cameraZoomSpeed;
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
            (5.0f / CurrentZoomLevel) *
            (float)latRange
            ) * m_cameraPanSpeedMultiplier;
    }

    private void PositionCamera()
    {
        double og_alt = m_currentAltitude;
        double og_lat = m_currentLat;
        double og_lon = m_currentLon;


        m_targetAltitude = Math.Clamp(m_targetAltitude, m_minCameraAltitude, m_maxCameraAltitude);
        double nextAlt = Mathf.Lerp(m_currentAltitude, m_targetAltitude, 0.1);
        m_currentAltitude = Math.Clamp(nextAlt, m_minCameraAltitude, m_maxCameraAltitude);

        m_currentLat = Mathf.LerpAngle(m_currentLat, m_targetLat, m_cameraPanSmoothing.Y);
        m_currentLon = Mathf.LerpAngle(m_currentLon, m_targetLon, m_cameraPanSmoothing.X);

        Position = MapUtils.LatLonToCartesianWGS84(m_currentLat, m_currentLon, m_currentAltitude);
        LookAt(Vector3.Zero, Vector3.Up);

        m_currentLat = (m_currentLat < -Math.PI) ?  Math.PI : m_currentLat;

        m_currentLon = (m_currentLon >  Math.PI) ? -Math.PI : m_currentLon;
        m_currentLon = (m_currentLon < -Math.PI) ? Math.PI : m_currentLon;

        if(og_alt != m_currentAltitude) EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
        if(og_lat != m_currentLat || og_lon != m_currentLon) EmitSignal(SignalName.OrbitalCameraLatLonChanged, Lat, Lon);

    }

    private void OnMachineCardClicked(Machine machine)
    {
        Altitude altMsg = (Altitude) machine.GetHellenicMessage(HellenicMessageType.Altitude);
        LatitudeLongitude latLonMsg = (LatitudeLongitude)machine.GetHellenicMessage(HellenicMessageType.LatitudeLongitude);
        if (altMsg == null || latLonMsg == null) return;

        m_targetLat = latLonMsg.Lat.HasValue ? Mathf.DegToRad(latLonMsg.Lat.Value) : m_currentLat;
        m_targetLon = latLonMsg.Lon.HasValue ? Mathf.DegToRad(latLonMsg.Lon.Value) : m_currentLon;

        EmitSignal(SignalName.OrbitalCameraLatLonChanged, Lat, Lon);
        EmitSignal(SignalName.OrbitalCameraAltChanged, CurrentAltitude);
    }

    private void OnZoomInButtonClicked()
    {
        m_targetAltitude /= 1.1;
    }

    private void OnZoomOutButtonClicked()
    {
        m_targetAltitude *= 1.1;
    }

    // Sets camera parameters based on the planet type
    public void SetPlanetParameters(PlanetShapeType planetType)
    {
        (double planetSemiMajorAxis, double planetSemiMinorAxis) = MapUtils.GetPlanetSemiMajorAxis(planetType);
        m_planetSemiMajorAxis = planetSemiMajorAxis;
        m_planetSemiMinorAxis = planetSemiMinorAxis;
        m_minCameraAltitude = 1.0d;
        m_maxCameraAltitude = planetSemiMajorAxis * m_maxAltitudeMultiplier;
        m_initialCameraAltitude = planetSemiMajorAxis * m_initialAltitudeMultiplier;
    }
}
