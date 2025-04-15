using System;
using Godot;

namespace Hermes.Common.HermesUtils;

/// <summary>
/// Some useful utilities for Godot related stuff that isn't available in the native Godot engine, or would require lots of
/// boilerplate to make.
/// </summary>
public static class HermesUtils
{
    private static bool m_infoLoggingEnabled = false;
    private static bool m_warningLoggingEnabled = false;
    private static bool m_errorLoggingEnabled = false;

    private const float MAX_RAYCAST_DISTANCE_CHECK = 2500;


    /// <summary>
    /// Checks if a Godot object is valid in the "Godot space", in "C# space", and if the object is not about to be
    /// deleted from memory via Godot's "queue_free()" function.
    /// </summary>
    /// <param name="node">The node to check. Must be a Godot object</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if the Godot object is valid and safe to access/use</returns>
    public static bool IsValid<T>(this T node) where T : Godot.GodotObject
    {
        // Though IsInstanceValid internally check
        return node != null
               && Godot.GodotObject.IsInstanceValid(node)
               && !node.IsQueuedForDeletion();
    }

    /// <summary>
    /// Determines whether the mouse pointer is in contact with a CollisionObject3D
    /// </summary>
    /// <param name="viewport">The viewport that the camera is in</param>
    /// <param name="objectToCheck">Collision object to check for contact with the mouse</param>
    /// <param name="layer">The layer that the collision object is on</param>
    /// <returns></returns>
    public static bool MouseHovering(Viewport viewport, CollisionObject3D objectToCheck, uint layer)
    {
        if (viewport == null || objectToCheck == null)
        {
            throw new ArgumentNullException("Cant determine if mouse event is clicked if the viewport or object to check is null!");
        }

        Camera3D camera = viewport.GetCamera3D();
        Vector2 mousePos = viewport.GetMousePosition();

        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayEnd = rayOrigin + camera.ProjectRayNormal(mousePos) * MAX_RAYCAST_DISTANCE_CHECK;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.CollideWithAreas = false;
        query.CollisionMask = layer;

        PhysicsDirectSpaceState3D spaceState = objectToCheck.GetWorld3D().DirectSpaceState;
        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

        GD.Print(result);

        return (result.Count > 0) && (result["collider"].Obj == objectToCheck);

    }

    public static void HermesLogInfo(string message)
    {
        if(!m_infoLoggingEnabled) return;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} ");
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void HermesLogWarning(string message)
    {
        if(!m_warningLoggingEnabled) return;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} ");
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void HermesLogError(string message)
    {
        if(!m_errorLoggingEnabled) return;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} ");
        Console.WriteLine(message);
        Console.ResetColor();
    }

}
