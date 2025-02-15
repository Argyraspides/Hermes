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
using Godot;

public partial class PlanetOrbitalCamera : Camera3D
{
    #region Camera Signals

    [Signal]
    public delegate void OrbitalCameraPosChangedEventHandler(Vector3 position);

    #endregion

    #region Camera Distance Configuration

    [Export] private float m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
    [Export] private float m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10;
    [Export] private float m_initialCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 5;

    // The current (smoothed) distance and the target distance. The current distance represents the distance from the
    // center of the Earth
    [Export] private float m_currentDistance;
    private float m_targetDistance;

    #endregion

    #region Camera Movement Configuration

    [Export] private float m_panSpeedMultiplier = 0.005f;
    [Export] private float m_panSmoothing = 0.1f; // Lower = slower smoothing

    [Export] private float m_zoomSmoothing = 0.15f; // Lower = slower smoothing
    [Export] private float m_baseZoomIncrement = 500.0f;

    [Export]
    private float m_zoomSensitivityFactor = 0.15f; // Controls how quickly zoom sensitivity changes with distance

    #endregion

    #region Camera Spherical Coordinates

    // We represent the camera position in spherical coordinates.
    // theta: azimuth (radians) - horizontal angle around the origin (longitude equivalent)
    // phi: polar (radians) - angle from the Y axis (0 = top) (latitude equivalent)
    private float m_currentTheta;
    private float m_currentPhi;

    // Latitude and longitude that the center of the camera is looking at
    [Export] public float CurrentLat { get; private set; }
    [Export] public float CurrentLon { get; private set; }

    // Approximate lat/lon radius range that the camera is able to see of the Earth's surface.
    // Calculated by via a vision cone, where the circumference
    // of the cones base (the cone's height is the distance from the camera to the Earth's surface)
    // is projected onto the Earth's surface. The range
    // of this circumference on the Earth's surface is turned into a lat/lon range.
    // This will slightly overestimate the truly visible lat/lon range,
    // but simplifies calculation drastically and improves performance. Used
    // to determine where to split the quadtree for LoD.
    [Export] public float ApproxVisibleLatRadius { get; private set; }
    [Export] public float ApproxVisibleLonRadius { get; private set; }

    private float m_targetTheta;
    private float m_targetPhi;

    #endregion

    #region Mouse Input State

    public bool IsDragging { get; set; } = false;

    #endregion

    #region Lifecycle Methods

    public override void _Ready()
    {
        // Initialize distance
        m_currentDistance = m_initialCameraRadialDistance;
        m_targetDistance = m_initialCameraRadialDistance;

        // Derive initial spherical coordinates from the current position.
        // If the position hasn't been set, we default to a standard view.
        if (Position == Vector3.Zero)
        {
            // Default angles: look from an angle (e.g., 45° down)
            m_currentTheta = Mathf.DegToRad(45);
            m_currentPhi = Mathf.DegToRad(45);
        }
        else
        {
            m_currentDistance = Position.Length();
            m_targetDistance = m_currentDistance;
            // theta: angle around Y
            m_currentTheta = Mathf.Atan2(Position.Z, Position.X);
            // phi: angle from the Y axis (avoid the poles)
            m_currentPhi = Mathf.Clamp(Mathf.Acos(Position.Y / m_currentDistance), 0.1f, Mathf.Pi - 0.1f);
        }

        // Start with target values equal to current values
        m_targetTheta = m_currentTheta;
        m_targetPhi = m_currentPhi;
    }

    public override void _Process(double delta)
    {
        // Smoothly update distance and angles
        m_currentDistance = Mathf.Lerp(m_currentDistance, m_targetDistance, m_zoomSmoothing);
        m_currentTheta = Mathf.LerpAngle(m_currentTheta, m_targetTheta, m_panSmoothing);
        m_currentPhi = Mathf.Lerp(m_currentPhi, m_targetPhi, m_panSmoothing);

        CurrentLon = m_currentTheta;
        CurrentLat = m_currentPhi;

        CurrentLon = -((CurrentLon % (2 * Mathf.Pi)) - Mathf.Pi);
        CurrentLat = -((CurrentLat % (Mathf.Pi)) - (Mathf.Pi / 2.0f));

        // Convert spherical coordinates back to Cartesian coordinates.
        // Spherical to Cartesian conversion:
        // x = r * sin(phi) * cos(theta)
        // y = r * cos(phi)
        // z = r * sin(phi) * sin(theta)
        Vector3 newPos = new Vector3(
            m_currentDistance * Mathf.Sin(m_currentPhi) * Mathf.Cos(m_currentTheta),
            m_currentDistance * Mathf.Cos(m_currentPhi),
            m_currentDistance * Mathf.Sin(m_currentPhi) * Mathf.Sin(m_currentTheta)
        );
        Position = newPos;
        LookAt(Vector3.Zero, Vector3.Up);

        UpdateVisibleLatLonRange();

        EmitSignal("OrbitalCameraPosChanged", Position);
    }

    public override void _Input(InputEvent @event)
    {
        // Handle panning input.
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                IsDragging = mouseButton.Pressed;
            }

            HandleZoomInput(mouseButton);
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (IsDragging)
            {
                HandlePanInput(mouseMotion);
            }
        }
    }

    void UpdateVisibleLatLonRange()
    {
        float fovRad = Mathf.DegToRad(Fov);
        float distanceFromEarthSurface = m_currentDistance - SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

        float circleViewRadius = distanceFromEarthSurface * Mathf.Tan(fovRad / 2.0f);
        (double lat, double lon) =
            MapUtils.DistanceToLatLonRange(circleViewRadius, SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
        ApproxVisibleLatRadius = Mathf.Clamp((float)lat, (float)MapUtils.MIN_LATITUDE, (float)MapUtils.MAX_LATITUDE);
        ApproxVisibleLonRadius = Mathf.Clamp((float)lon, (float)MapUtils.MIN_LONGITUDE, (float)MapUtils.MAX_LONGITUDE);
    }

    #endregion

    #region Input Handlers

    private void HandlePanInput(InputEventMouseMotion mouseMotion)
    {
        // Calculate dynamic pan speed based on current distance from Earth's surface
        float distanceFromSurface = m_currentDistance - SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        float dynamicPanMultiplier = m_panSpeedMultiplier *
                                     (distanceFromSurface / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM) *
                                     m_zoomSensitivityFactor;

        // Ensure minimum pan speed for very close distances
        dynamicPanMultiplier = Mathf.Max(dynamicPanMultiplier, m_panSpeedMultiplier * 0.01f);

        float deltaX = -mouseMotion.Relative.X * dynamicPanMultiplier;
        float deltaY = -mouseMotion.Relative.Y * dynamicPanMultiplier;

        m_targetTheta -= deltaX;
        m_targetPhi += deltaY;

        // Clamp phi so that we don't flip over at the poles.
        m_targetPhi = Mathf.Clamp(m_targetPhi, 0.1f, Mathf.Pi - 0.1f);
    }

    private void HandleZoomInput(InputEventMouseButton mouseButton)
    {
        // Calculate dynamic zoom increment based on current distance from Earth's surface
        float distanceFromSurface = m_currentDistance - SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        float dynamicZoomIncrement = m_baseZoomIncrement *
                                     (distanceFromSurface / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM) *
                                     m_zoomSensitivityFactor;

        // Ensure minimum zoom increment for very close distances
        dynamicZoomIncrement = Mathf.Max(dynamicZoomIncrement, m_baseZoomIncrement * 0.01f);

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            m_targetDistance = Mathf.Max(m_minCameraRadialDistance, m_targetDistance - dynamicZoomIncrement);
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            m_targetDistance = Mathf.Min(m_maxCameraRadialDistance, m_targetDistance + dynamicZoomIncrement);
        }
    }

    #endregion

    #region Public API

    public void InitializeCameraPosition(Vector3 position)
    {
        // This method sets the camera to a given position and recalculates spherical coordinates.
        Position = position;
        m_currentDistance = position.Length();
        m_targetDistance = m_currentDistance;
        m_currentTheta = Mathf.Atan2(position.Z, position.X);
        m_currentPhi = Mathf.Clamp(Mathf.Acos(position.Y / m_currentDistance), 0.1f, Mathf.Pi - 0.1f);

        m_targetTheta = m_currentTheta;
        m_targetPhi = m_currentPhi;

        LookAt(Vector3.Zero, Vector3.Up);
    }

    #endregion
}
