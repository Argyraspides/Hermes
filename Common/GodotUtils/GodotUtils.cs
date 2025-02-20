/// <summary>
/// Some useful utilities for Godot related stuff that isn't available in the native Godot engine, or would require lots of
/// boilerplate to make.
/// </summary>
public static class GodotUtils
{
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
}
