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


using Godot;

public partial class PlanetOrbitalCamera : Camera3D
{
    #region Camera Signals

    [Signal]
    public delegate void OrbitalCameraPosChangedEventHandler(Vector3 position, float latitude, float longitude);

    #endregion

    #region Camera Distance Configuration

    [Export] private double m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
    [Export] private double m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10;
    [Export] private double m_initialCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 5;
    [Export] private double MIN_HEIGHT_ABOVE_ELLIPSOID = 0.03F; // kilometers

    // The current (smoothed) distance and the target distance. The current distance represents the distance from the
    // center of the Earth
    [Export] private double m_currentDistance;
    private double m_targetDistance;

    #endregion

    #region Camera Movement Configuration

    [Export] private double m_panSpeedMultiplier = 0.005f;
    [Export] private double m_panSmoothing = 0.1f; // Lower = slower smoothing

    [Export] private double m_zoomSmoothing = 0.15f; // Lower = slower smoothing
    [Export] private double m_baseZoomIncrement = 500.0f;

    [Export]
    private double m_zoomSensitivityFactor = 0.15f; // Controls how quickly zoom sensitivity changes with distance

    #endregion

    #region Camera Spherical Coordinates

    // We represent the camera position in spherical coordinates.
    // theta: azimuth (radians) - horizontal angle around the origin (longitude equivalent)
    // phi: polar (radians) - angle from the Y axis (0 = top) (latitude equivalent)
    private double m_currentTheta;
    private double m_currentPhi;

    // Latitude and longitude that the center of the camera is looking at
    [Export] public double CurrentLat { get; private set; }
    [Export] public double CurrentLon { get; private set; }

    // Approximate lat/lon radius range that the camera is able to see of the Earth's surface.
    // Calculated by via a vision cone, where the circumference
    // of the cones base (the cone's height is the distance from the camera to the Earth's surface)
    // is projected onto the Earth's surface. The range
    // of this circumference on the Earth's surface is turned into a lat/lon range.
    // This will slightly overestimate the truly visible lat/lon range,
    // but simplifies calculation drastically and improves performance. Used
    // to determine where to split the quadtree for LoD.
    [Export] public double ApproxVisibleLatRadius { get; private set; }
    [Export] public double ApproxVisibleLonRadius { get; private set; }

    private double m_targetTheta;
    private double m_targetPhi;

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
        // Ensure we maintain minimum distance even during smooth transitions
        double currentMinDistance = GetMinimumAllowedDistance();
        m_currentDistance = Mathf.Max(currentMinDistance, m_currentDistance);
        m_targetDistance = Mathf.Max(currentMinDistance, m_targetDistance);

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
            (float)m_currentDistance * Mathf.Sin((float)m_currentPhi) * Mathf.Cos((float)m_currentTheta),
            (float)m_currentDistance * Mathf.Cos((float)m_currentPhi),
            (float)m_currentDistance * Mathf.Sin((float)m_currentPhi) * Mathf.Sin((float)m_currentTheta)
        );
        Position = newPos;
        LookAt(Vector3.Zero, Vector3.Up);

        UpdateVisibleLatLonRange();

        EmitSignal("OrbitalCameraPosChanged", Position, CurrentLat, CurrentLon);
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
        double fovRad = Mathf.DegToRad(Fov);
        double distanceFromEarthSurface = m_currentDistance - SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

        double circleViewRadius = distanceFromEarthSurface * Mathf.Tan(fovRad / 2.0f);
        (double lat, double lon) =
            MapUtils.DistanceToLatLonRange(circleViewRadius, SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
        ApproxVisibleLatRadius = Mathf.Clamp(lat, MapUtils.MIN_LATITUDE, MapUtils.MAX_LATITUDE);
        ApproxVisibleLonRadius = Mathf.Clamp(lon, MapUtils.MIN_LONGITUDE, MapUtils.MAX_LONGITUDE);
    }


    private double GetWGS84RadiusAtLatitude(double latitude)
    {
        // WGS84 ellipsoid radius calculation at a given latitude
        double a = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        double b = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM;
        double cos = Mathf.Cos(latitude);
        double sin = Mathf.Sin(latitude);

        double t1 = a * a * cos;
        double t2 = b * b * sin;
        double t3 = a * cos;
        double t4 = b * sin;

        return Mathf.Sqrt((t1 * t1 + t2 * t2) / (t3 * t3 + t4 * t4));
    }

    private double GetMinimumAllowedDistance()
    {
        // Convert the WGS84 radius to the same units as solar system
        double radius = GetWGS84RadiusAtLatitude(CurrentLat);
        return radius + MIN_HEIGHT_ABOVE_ELLIPSOID;
    }

    #endregion

    #region Input Handlers

    private void HandlePanInput(InputEventMouseMotion mouseMotion)
    {
        // Calculate dynamic pan speed based on current distance from Earth's surface
        double distanceFromSurface = m_currentDistance - SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        double dynamicPanMultiplier = m_panSpeedMultiplier *
                                      (distanceFromSurface / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM) *
                                      m_zoomSensitivityFactor;

        // Ensure minimum pan speed for very close distances
        dynamicPanMultiplier = Mathf.Max(dynamicPanMultiplier, m_panSpeedMultiplier * 0.01f);

        double deltaX = -mouseMotion.Relative.X * dynamicPanMultiplier;
        double deltaY = -mouseMotion.Relative.Y * dynamicPanMultiplier;

        m_targetTheta -= deltaX;
        m_targetPhi += deltaY;

        // Clamp phi so that we don't flip over at the poles.
        m_targetPhi = Mathf.Clamp(m_targetPhi, 0.1f, Mathf.Pi - 0.1f);
    }

    private void HandleZoomInput(InputEventMouseButton mouseButton)
    {
        // Calculate dynamic zoom increment based on current distance from Earth's surface
        double currentMinDistance = GetMinimumAllowedDistance();
        double distanceFromSurface = m_currentDistance - currentMinDistance;
        double dynamicZoomIncrement = m_baseZoomIncrement *
                                      (distanceFromSurface / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM) *
                                      m_zoomSensitivityFactor;

        // Ensure minimum zoom increment for very close distances
        dynamicZoomIncrement = Mathf.Max(dynamicZoomIncrement, m_baseZoomIncrement * 0.01f);

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            m_targetDistance = Mathf.Max(currentMinDistance, m_targetDistance - dynamicZoomIncrement);
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
