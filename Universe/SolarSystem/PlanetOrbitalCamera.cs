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
    public delegate void OrbitalCameraPosChangedEventHandler(Vector3 position);

    #endregion

    #region Camera Distance Configuration
    [Export] private float m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
    [Export] private float m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10;
    [Export] private float m_cameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 5;
    private float m_targetCameraRadialDistance;
    #endregion

    #region Camera Movement Configuration
    [Export] private float m_cameraPanSpeedMultiplier = 20.0f;
    [Export] private float m_cameraPanSmoothingMultiplier = 5.0f;

    [Export] private float m_cameraZoomSmoothingMultiplier = 5.0f;
    [Export] private float m_cameraZoomIncrement = 500.0f;
    #endregion

    #region Camera Visibility & Position Information
    private float m_approxVisibleLatitudeRange; // Visible latitude range of the Earth given the camera's distance & FOV
    private float m_approxVisibleLongitudeRange; // Visible longitude range of the Earth given the camera's distance & FOV
    private float m_cameraLatitude; // Line of latitude that the center of the camera is looking at
    private float m_cameraLongitude; // Line of longitude that the center of the camera is looking at
    #endregion

    #region Mouse Input State
    private bool m_isDragging = false;
    private Vector2 m_dragStartPos;
    private Vector3 m_targetCameraPanPosition;
    #endregion

    #region Lifecycle Methods
    public override void _Ready()
    {
        m_targetCameraRadialDistance = m_cameraRadialDistance;
        m_targetCameraPanPosition = Position;
    }

    public override void _Process(double delta)
    {
        UpdateCameraDistance((float)delta);
        UpdateCameraOrientation((float)delta);
        UpdateVisibleLonLat();
    }

    public override void _Input(InputEvent @event)
    {
        HandleCameraPanningInput(@event);
        HandleCameraZoomingInput(@event);
    }
    #endregion

    #region Camera Position Updates
    private void UpdateCameraDistance(float delta)
    {
        m_cameraRadialDistance = Mathf.Lerp(
            m_cameraRadialDistance,
            m_targetCameraRadialDistance,
            (float)delta * m_cameraZoomSmoothingMultiplier
        );

        Vector3 normalizedVec = Position.Normalized();

        Vector3 newPos = new Vector3(
            m_cameraRadialDistance * normalizedVec.X,
            m_cameraRadialDistance * normalizedVec.Y,
            m_cameraRadialDistance * normalizedVec.Z
        );
        Position = newPos;
        LookAt(Vector3.Zero, Vector3.Up);
    }

    private void UpdateCameraOrientation(float delta)
    {
        float slerpWeight = delta * m_cameraPanSmoothingMultiplier;
        slerpWeight = (float)Mathf.Clamp(slerpWeight, 0.0, 1.0);
        Position = Position.Slerp(
            m_targetCameraPanPosition,
            slerpWeight
        );
        LookAt(Vector3.Zero, Vector3.Up);
    }

    private void UpdateCameraPanTargetPosition(Vector2 delta)
    {
        // Convert current camera position to spherical coordinates
        float radius = m_cameraRadialDistance;
        float theta = Mathf.Atan2(Position.Z, Position.X);  // Azimuthal angle, longitude equivalent
        float phi = Mathf.Acos(Position.Y / radius);        // Polar angle, latitude equivalent

        m_cameraLatitude = Mathf.Acos(-Position.Y / radius) - (Mathf.Pi / 2);
        m_cameraLongitude = -Mathf.Atan2(-Position.Z, -Position.X);

        // Update angles based on mouse movement
        float deltaTime = (float)GetProcessDeltaTime();
        theta -= delta.X * m_cameraPanSpeedMultiplier * deltaTime;
        phi += delta.Y * m_cameraPanSpeedMultiplier * deltaTime;
        phi = Mathf.Clamp(phi, 0.1f, Mathf.Pi - 0.1f);     // Prevent camera flip at poles

        // Convert back to Cartesian coordinates
        m_targetCameraPanPosition = new Vector3(
            radius * Mathf.Sin(phi) * Mathf.Cos(theta),
            radius * Mathf.Cos(phi),
            radius * Mathf.Sin(phi) * Mathf.Sin(theta)
        );
    }

    private void UpdateVisibleLonLat()
    {
        // Radius of the circular area we can see on a flat surface given camera FOV and
        // distance away from the flat surface
        float fovRadians = Mathf.DegToRad(Fov);
        float viewCircleRad = (float)(m_cameraRadialDistance * Mathf.Tan(fovRadians));

        (double lat, double lon) =
            MapUtils.DistanceToLatLonRange(
                (double)viewCircleRad,
                SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM
            );
        m_approxVisibleLatitudeRange = (float)lat;
        m_approxVisibleLongitudeRange = (float)lon;
    }

    #endregion

    #region Input Handlers
    private void HandleCameraPanningInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButtonPanInput(mouseButton);
            EmitSignal("OrbitalCameraPosChanged", Position);
        }
        else if (@event is InputEventMouseMotion mouseMotion && m_isDragging)
        {
            HandleMouseMotionPanInput(mouseMotion);
            EmitSignal("OrbitalCameraPosChanged", Position);
        }
    }

    private void HandleMouseButtonPanInput(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            m_isDragging = mouseButton.Pressed;
            if (m_isDragging)
            {
                m_dragStartPos = mouseButton.Position;
                EmitSignal("OrbitalCameraPosChanged", Position);
            }
        }
    }

    private void HandleMouseMotionPanInput(InputEventMouseMotion mouseMotion)
    {
        Vector2 dragDelta = m_dragStartPos - mouseMotion.Position;
        m_dragStartPos = mouseMotion.Position;
        UpdateCameraPanTargetPosition(dragDelta.Normalized());
    }

    private void HandleCameraZoomingInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelUp && m_cameraRadialDistance > m_minCameraRadialDistance)
        {
            m_targetCameraRadialDistance = Mathf.Max(
                m_minCameraRadialDistance,
                m_targetCameraRadialDistance - m_cameraZoomIncrement
            );
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown && m_cameraRadialDistance < m_maxCameraRadialDistance)
        {
            m_targetCameraRadialDistance = Mathf.Min(
                m_maxCameraRadialDistance,
                m_targetCameraRadialDistance + m_cameraZoomIncrement
            );
        }
    }
    #endregion

    #region Initialization
    public void InitializeCameraPosition(Vector3 position)
    {
        Position = position;

        // TODO(Argyraspides, 08/02/2025)
        // Somehow make this intelligent to automatically determine lat/lon based on world position, or explicitly pass them in
        m_cameraLatitude = 0.0f;
        m_cameraLongitude = 0.0f;
        LookAt(Vector3.Zero, Vector3.Up);
    }
    #endregion


}
