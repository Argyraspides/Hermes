using Godot;
using System;

public partial class PlanetOrbitalCamera : Camera3D
{

    // TODO: Since we are using an ellipsoid and not a sphere, the distance to the surface is not 
    // the same from all angles. Change this to be dynamic based on lat/long of camera
    [Export]
    private float minCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

    [Export]
    private float maxCameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 4;

    // Distance of the camera from the Earth's center. 
    private float cameraRadialDistance = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 4;

    // Is the user currently dragging their mouse across the screen?
    private bool isDragging = false;
    // If they're dragging, where is their mouse currently?
    private Vector2 dragStartPos;

    // How fast does the camera pan (multiplier)?
    [Export]
    private float cameraPanSpeedMultiplier = 5.0f;

    // How fast does the camera zoom in/out (absolute)?
    // TODO: Change this so that the zooming increments decrease as we get closer to the surface
    [Export]
    private float cameraZoomIncrement = 50.0f;




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
                    isDragging = true;
                    dragStartPos = mouseButton.Position;
                }
                else
                {
                    isDragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent && isDragging)
        {

            Vector2 dragDelta = dragStartPos - mouseMotionEvent.Position;
            dragStartPos = mouseMotionEvent.Position;
            Vector2 dragDeltaNormalized = dragDelta.Normalized();
            PanCamera(dragDeltaNormalized);
        }
    }

    private void PanCamera(Vector2 delta)
    {
        // Convert the current camera position to spherical coordinates
        float radius = cameraRadialDistance;                // We keep this constant
        float theta = Mathf.Atan2(Position.Z, Position.X);  // Azimuthal angle (horizontal rotation)
        float phi = Mathf.Acos(Position.Y / radius);        // Polar angle (vertical rotation)

        // Apply the mouse movement to our angles
        // Negative delta.x for natural feeling left/right movement
        theta -= delta.X * cameraPanSpeedMultiplier * (float)GetProcessDeltaTime();
        // Negative delta.y for natural feeling up/down movement
        phi += delta.Y * cameraPanSpeedMultiplier * (float)GetProcessDeltaTime();

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
                if(cameraRadialDistance <= minCameraRadialDistance) return;
                cameraRadialDistance -= cameraZoomIncrement;
                ZoomCamera(cameraRadialDistance);
            }
            else if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
            {
                if(cameraRadialDistance >= maxCameraRadialDistance) return;
                cameraRadialDistance += cameraZoomIncrement;
                ZoomCamera(cameraRadialDistance);
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
            0, 0, cameraRadialDistance
        );
        Position = startPos;
    }
}