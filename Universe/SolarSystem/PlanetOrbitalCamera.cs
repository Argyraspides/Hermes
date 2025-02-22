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

using Hermes.Common.Map.Utils;

namespace Hermes.Universe.SolarSystem;

using System;
using System.Linq;
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
    [Export] private double MIN_HEIGHT_ABOVE_ELLIPSOID = 0.1F; // kilometers

    // The current (smoothed) distance and the target distance. The current distance represents the distance from the
    // center of the Earth
    [Export] private double m_currentDistance;
    private double m_targetDistance;

    #endregion

    #region Camera Movement Configuration

    [Export] private double m_panSpeedMultiplier = 0.005f;
    [Export] private double m_panSmoothing = 0.1f; // Lower = slower smoothing

    [Export] private double m_zoomSmoothing = 0.15f; // Lower = slower smoothing
    [Export] private double m_baseZoomIncrement = 1.0f; // Base zoom is now a factor, not a fixed amount

    [Export] private double m_zoomSensitivityFactor = 0.15f; // Controls how quickly zoom sensitivity changes

    public int CurrentZoomLevel;

    private readonly double[] m_baseAltitudeThresholds = new double[]
    {
        156000.0f, 78000.0f, 39000.0f, 19500.0f, 9750.0f, 4875.0f, 2437.5f, 1218.75f, 609.375f, 304.6875f, 152.34f,
        76.17f, 38.08f, 19.04f, 9.52f, 4.76f, 2.38f, 1.2f, 0.6f, 0.35f
    };

    private const double ZoomIncrementFactor = 0.2f; // 2/10th of the distance difference as zoom increment

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
        if (Position == Vector3.Zero)
        {
            // Default angles
            m_currentTheta = Mathf.DegToRad(45);
            m_currentPhi = Mathf.DegToRad(45);
        }
        else
        {
            m_currentDistance = Position.Length();
            m_targetDistance = m_currentDistance;
            m_currentTheta = Mathf.Atan2(Position.Z, Position.X);
            m_currentPhi = Mathf.Clamp(Mathf.Acos(Position.Y / m_currentDistance), 0.1f, Mathf.Pi - 0.1f);
        }

        // Start with target values equal to current values
        m_targetTheta = m_currentTheta;
        m_targetPhi = m_currentPhi;
    }

    public override void _Process(double delta)
    {
        // Ensure minimum distance
        double currentMinDistance = GetMinimumAllowedDistance();
        m_currentDistance = Mathf.Max(currentMinDistance, m_currentDistance);
        m_targetDistance = Mathf.Max(currentMinDistance, m_targetDistance);

        m_currentDistance = Mathf.Lerp(m_currentDistance, m_targetDistance, m_zoomSmoothing);
        m_currentTheta = Mathf.LerpAngle(m_currentTheta, m_targetTheta, m_panSmoothing);
        m_currentPhi = Mathf.Lerp(m_currentPhi, m_targetPhi, m_panSmoothing);

        CurrentLon = m_currentTheta;
        CurrentLat = m_currentPhi;

        CurrentLon = -((CurrentLon % (2 * Mathf.Pi)) - Mathf.Pi);
        CurrentLat = Mathf.Clamp(-((CurrentLat % (Mathf.Pi)) - (Mathf.Pi / 2.0f)), -Mathf.Pi / 2.0f,
            Mathf.Pi / 2.0f); // Clamped latitude

        // Convert spherical coordinates back to Cartesian coordinates.
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
        // Calculate pan increment based on visible latitude/longitude range
        double panIncrementLat = ApproxVisibleLatRadius;
        double panIncrementLon = ApproxVisibleLonRadius;

        // Apply pan speed multiplier and sensitivity factor
        double dynamicPanMultiplierLat = panIncrementLat * m_panSpeedMultiplier * m_zoomSensitivityFactor;
        double dynamicPanMultiplierLon = panIncrementLon * m_panSpeedMultiplier * m_zoomSensitivityFactor;


        // Ensure minimum pan speed (optional, but keeps *some* movement even when very zoomed in/small visible range)
        dynamicPanMultiplierLat =
            Mathf.Max(dynamicPanMultiplierLat, m_panSpeedMultiplier * 0.0001f);
        dynamicPanMultiplierLon = Mathf.Max(dynamicPanMultiplierLon, m_panSpeedMultiplier * 0.0001f);

        double deltaPhi =
            -mouseMotion.Relative.Y * dynamicPanMultiplierLat; // Vertical mouse motion changes latitude (phi)
        double deltaTheta =
            -mouseMotion.Relative.X * dynamicPanMultiplierLon; // Horizontal mouse motion changes longitude (theta)


        m_targetPhi += deltaPhi;
        m_targetTheta -=
            deltaTheta;

        // Clamp phi
        m_targetPhi = Mathf.Clamp(m_targetPhi, 0.1f, Mathf.Pi - 0.1f);
    }

    private void HandleZoomInput(InputEventMouseButton mouseButton)
    {
        double currentMinDistance = GetMinimumAllowedDistance();
        double currentDistanceAboveSurface = m_currentDistance - currentMinDistance;

        // Determine current zoom level based on altitude thresholds
        CurrentZoomLevel = GetZoomLevelForDistance(currentDistanceAboveSurface);

        // Calculate zoom increment based on distance to the next/previous zoom level
        double dynamicZoomIncrement = CalculateZoomIncrementForLevel(CurrentZoomLevel,
            mouseButton.ButtonIndex == MouseButton.WheelUp);


        if (mouseButton.ButtonIndex == MouseButton.WheelUp) // Zoom In
        {
            m_targetDistance = Mathf.Max(currentMinDistance, m_targetDistance - dynamicZoomIncrement);
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown) // Zoom Out
        {
            m_targetDistance = Mathf.Min(m_maxCameraRadialDistance, m_targetDistance + dynamicZoomIncrement);
        }
    }

    private int GetZoomLevelForDistance(double distanceAboveSurface)
    {
        for (int i = 0; i < m_baseAltitudeThresholds.Length; i++)
        {
            if (distanceAboveSurface > m_baseAltitudeThresholds[i])
            {
                return i; //  Return the zoom level (index in the array)
            }
        }

        return m_baseAltitudeThresholds.Length; // Closest zoom level if below all thresholds
    }

    private double CalculateZoomIncrementForLevel(int zoomLevel, bool zoomIn)
    {
        int next = zoomIn ? 1 : -1;
        int zLevelIdx = zoomLevel - 1;
        double nextThreshold = (zLevelIdx + next < m_baseAltitudeThresholds.Length && zLevelIdx + next >= 0)
            ? m_baseAltitudeThresholds[zLevelIdx + next]
            : 0;

        double currentThreshold = m_baseAltitudeThresholds[zLevelIdx];

        double distanceToNextLevel;

        if (zoomIn)
        {
            distanceToNextLevel =
                Math.Abs(currentThreshold -
                         nextThreshold); // Distance to next *higher detail* level (lower altitude)
            if (zoomLevel + 1 >= m_baseAltitudeThresholds.Length) // Already at max zoom, just use a small increment
            {
                return m_baseZoomIncrement * m_zoomSensitivityFactor * 0.1f; // Very small increment at max zoom
            }
        }
        else // Zoom out
        {
            distanceToNextLevel =
                Math.Abs(nextThreshold -
                         currentThreshold); // Distance to next *lower detail* level (higher altitude)
            if (zoomLevel == 0) // Already at min zoom, limit zoom out.
            {
                return m_baseZoomIncrement * m_zoomSensitivityFactor * 0.1f; // Very small increment at min zoom
            }
        }

        double final = distanceToNextLevel * ZoomIncrementFactor * m_zoomSensitivityFactor *
                       m_baseZoomIncrement;
        return Math.Clamp(final, m_baseAltitudeThresholds.Last(),
            double.MaxValue); // Zoom increment is fraction of distance
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
