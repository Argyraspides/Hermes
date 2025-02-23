using Godot;
using System;
using Hermes.Universe.Autoloads;

public partial class CameraLatLonLabel : RichTextLabel
{
    private string degreeSymbol = "\u00b0";

    public override void _Ready()
    {
        EventBus.Instance.PlanetOrbitalCameraLatLonChanged += UpdateCameraLatLonLabel;
    }

    private void UpdateCameraLatLonLabel(double latitude, double longitude)
    {
        latitude = Mathf.RadToDeg(latitude);
        longitude = Mathf.RadToDeg(longitude);
        Text =
            latitude.ToString("F6") + degreeSymbol +
            ", " +
            longitude.ToString("F6") + degreeSymbol;
    }
}
