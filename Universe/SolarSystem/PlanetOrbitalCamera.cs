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
    #region Camera Distance Configuration
    [Export] private float m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
    [Export] private float m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10;
    [Export] private float m_cameraRadialDistance = 18539;
    private float m_targetCameraRadialDistance;
    #endregion

    #region Camera Movement Configuration
    [Export] private float m_cameraPanSpeedMultiplier = 20.0f;
    [Export] private float m_cameraPanSmoothingMultiplier = 5.0f;

    [Export] private float m_cameraZoomSmoothingMultiplier = 5.0f;
    [Export] private float m_cameraZoomIncrement = 500.0f;
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

        InitializeCameraPosition();
    }

    public override void _Process(double delta)
    {
        UpdateCameraDistance((float)delta);
        UpdateCameraOrientation((float)delta);
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
        Position = Position.Slerp(
            m_targetCameraPanPosition,
            (float)delta * m_cameraPanSmoothingMultiplier
        );
        LookAt(Vector3.Zero, Vector3.Up);
    }

    private void UpdateCameraPanTargetPosition(Vector2 delta)
    {
        // Convert current camera position to spherical coordinates
        float radius = m_cameraRadialDistance;
        float theta = Mathf.Atan2(Position.Z, Position.X);  // Azimuthal angle
        float phi = Mathf.Acos(Position.Y / radius);        // Polar angle

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
    #endregion

    #region Input Handlers
    private void HandleCameraPanningInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButtonPanInput(mouseButton);
        }
        else if (@event is InputEventMouseMotion mouseMotion && m_isDragging)
        {
            HandleMouseMotionPanInput(mouseMotion);
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
    private void InitializeCameraPosition()
    {
        Position = new Vector3(0, 0, m_cameraRadialDistance);
    }
    #endregion
}
