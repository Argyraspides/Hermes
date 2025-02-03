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
    [Export]
    private float m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

    [Export]
    private float m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10;

    // Distance of the camera from the Earth's *center*.
    [Export]
    private float m_cameraRadialDistance = 18539;

    // Is the user currently dragging their mouse across the screen?
    private bool m_isDragging = false;

    // If they're dragging their mouse across the screen to pan, where is their mouse currently?
    private Vector2 m_dragStartPos;

    // How fast does the camera pan? (Multiplier)
    [Export]
    private float m_cameraPanSpeedMultiplier = 5.0f;

    // Smooth zooming linear interpolation factor
    [Export]
    private float m_cameraZoomSmoothingMultiplier = 5.0f;

    // How fast does the camera zoom in/out?
    [Export]
    private float m_cameraZoomIncrement = 500.0f;

    // Target distance for the camera to zoom to for smooth zooming in/out
    // via linear interpolation
    private float m_targetCameraRadialDistance;

    private Vector3 m_targetCameraPanPosition;

    // Smooth panning linear interpolation factor
    [Export]
    private float m_cameraPanSmoothingMultiplier = 5.0f;

    public override void _Ready()
    {
        m_targetCameraRadialDistance = m_cameraRadialDistance;
        m_targetCameraPanPosition = Position;
    }

    public override void _Process(double delta)
    {
        m_cameraRadialDistance = Mathf.Lerp(m_cameraRadialDistance, m_targetCameraRadialDistance, (float)delta * m_cameraZoomSmoothingMultiplier);
        UpdateCameraRadialDistance(m_cameraRadialDistance);

        Position = Position.Slerp(m_targetCameraPanPosition, (float)delta * m_cameraPanSmoothingMultiplier);
        LookAt(Vector3.Zero, Vector3.Up);

    }

    public override void _Input(InputEvent @event)
    {
        HandleCameraPanningInput(@event);
        HandleCameraZoomingInput(@event);
    }

    private void HandleCameraPanningInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    m_isDragging = true;
                    m_dragStartPos = mouseButton.Position;
                }
                else
                {
                    m_isDragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent && m_isDragging)
        {

            Vector2 dragDelta = m_dragStartPos - mouseMotionEvent.Position;
            m_dragStartPos = mouseMotionEvent.Position;
            Vector2 dragDeltaNormalized = dragDelta.Normalized();
            UpdateCameraPanTargetPosition(dragDeltaNormalized);
        }
    }

    private void UpdateCameraPanTargetPosition(Vector2 delta)
    {
        // Convert the current camera position to spherical coordinates
        float radius = m_cameraRadialDistance;              // We keep this constant
        float theta = Mathf.Atan2(Position.Z, Position.X);  // Azimuthal angle (horizontal rotation)
        float phi = Mathf.Acos(Position.Y / radius);        // Polar angle (vertical rotation)

        // Apply the mouse movement to our angles
        // Negative delta.x for natural feeling left/right movement
        theta -= delta.X * m_cameraPanSpeedMultiplier * (float)GetProcessDeltaTime();
        // Negative delta.y for natural feeling up/down movement
        phi += delta.Y * m_cameraPanSpeedMultiplier * (float)GetProcessDeltaTime();

        // Clamp the polar angle to prevent camera flipping over the poles
        phi = Mathf.Clamp(phi, 0.1f, Mathf.Pi - 0.1f);

        // Convert back to Cartesian coordinates
        Vector3 newPos = new Vector3(
            radius * Mathf.Sin(phi) * Mathf.Cos(theta),
            radius * Mathf.Cos(phi),
            radius * Mathf.Sin(phi) * Mathf.Sin(theta)
        );

        m_targetCameraPanPosition = newPos;
    }


    private void HandleCameraZoomingInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp)
            {
                if (m_cameraRadialDistance <= m_minCameraRadialDistance)
                {
                    return;
                }
                m_targetCameraRadialDistance = Mathf.Max(m_minCameraRadialDistance, m_targetCameraRadialDistance - m_cameraZoomIncrement);
            }
            else if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
            {
                if (m_cameraRadialDistance >= m_maxCameraRadialDistance)
                {
                    return;
                }
                m_targetCameraRadialDistance = Mathf.Max(m_minCameraRadialDistance, m_targetCameraRadialDistance + m_cameraZoomIncrement);
            }
        }
    }

    private void UpdateCameraRadialDistance(float distance)
    {
        Vector3 normalizedVec = Position.Normalized();
        float distXComponent = distance * normalizedVec.X;
        float distYComponent = distance * normalizedVec.Y;
        float distZComponent = distance * normalizedVec.Z;
        Vector3 newPos = new Vector3(
            distXComponent,
            distYComponent,
            distZComponent
        );
        Position = newPos;
        LookAt(Vector3.Zero, Vector3.Up);

    }

    private void InitializeCameraPosition()
    {
        Vector3 startPos = new Vector3(
            0, 0, m_cameraRadialDistance
        );
        Position = startPos;
    }
}
