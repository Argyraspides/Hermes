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
using System;

public partial class PlanetOrbitalCamera : Camera3D
{

    // TODO: Since we are using an ellipsoid and not a sphere, the distance to the surface is not
    // the same from all angles. Change this to be dynamic based on lat/long of camera
    [Export]
    private float m_minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

    [Export]
    private float m_maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 4;

    // Distance of the camera from the Earth's center.
    private float m_cameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 4;

    // Is the user currently dragging their mouse across the screen?
    private bool m_isDragging = false;
    // If they're dragging, where is their mouse currently?
    private Vector2 m_dragStartPos;

    // How fast does the camera pan (multiplier)?
    [Export]
    private float m_cameraPanSpeedMultiplier = 5.0f;

    // How fast does the camera zoom in/out (absolute)?
    // TODO: Change this so that the zooming increments decrease as we get closer to the surface
    [Export]
    private float m_cameraZoomIncrement = 50.0f;




    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
        HandleCameraPanning(@event);
        HandleCameraZooming(@event);
    }

    private void HandleCameraPanning(InputEvent @event)
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
            PanCamera(dragDeltaNormalized);
        }
    }

    private void PanCamera(Vector2 delta)
    {
        // Convert the current camera position to spherical coordinates
        float radius = m_cameraRadialDistance;                // We keep this constant
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

        Position = newPos;

        // Always look at the center (0,0,0) where the planet is
        LookAt(Vector3.Zero, Vector3.Up);
    }


    private void HandleCameraZooming(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp)
            {
                if(m_cameraRadialDistance <= m_minCameraRadialDistance) return;
                m_cameraRadialDistance -= m_cameraZoomIncrement;
                ZoomCamera(m_cameraRadialDistance);
            }
            else if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
            {
                if(m_cameraRadialDistance >= m_maxCameraRadialDistance) return;
                m_cameraRadialDistance += m_cameraZoomIncrement;
                ZoomCamera(m_cameraRadialDistance);
            }
        }
    }

    private void ZoomCamera(float distance)
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
    }

    private void InitializeCameraPosition()
    {
        Vector3 startPos = new Vector3(
            0, 0, m_cameraRadialDistance
        );
        Position = startPos;
    }
}
