public static class GodotUtils
{
    public static bool IsValid<T>(this T node) where T : Godot.GodotObject
    {
        return node != null
               && Godot.GodotObject.IsInstanceValid(node)
               && !node.IsQueuedForDeletion();
    }
}
