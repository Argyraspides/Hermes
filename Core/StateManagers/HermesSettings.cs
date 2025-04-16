using Godot;

namespace Hermes.Core.StateManagers;

/// <summary>
/// Settings that are hidden from the user (not configurable)
/// </summary>
public static partial class HermesSettings
{
    // The layer that 3D objects must be in if they want to be selectable by the mouse
    // (used for raycasting in HermesUtils)
    public const uint SELECTABLE_LAYER = 7;

    public const int MINIMUM_SCREEN_WIDTH = 1280;
    public const int MINIMUM_SCREEN_HEIGHT = 720;
}
