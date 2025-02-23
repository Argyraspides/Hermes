namespace Hermes.Universe.UI.UIComponents;

using Godot;
using Hermes.Universe.Autoloads;

public partial class CameraDistanceLabel : RichTextLabel
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        EventBus.Instance.PlanetOrbitalCameraAltChanged += UpdateCameraAltitudeLabel;
    }

    private void UpdateCameraAltitudeLabel(double altitude)
    {
        Text = "Camera: " + altitude.ToString("F3") + " km";
    }
}
