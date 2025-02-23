/*

          db         88888888ba     ,ad8888ba,    88           88           ,ad8888ba,
         d88b        88      "8b   d8"'    `"8b   88           88          d8"'    `"8b
        d8'`8b       88      ,8P  d8'        `8b  88           88         d8'        `8b
       d8'  `8b      88aaaaaa8P'  88          88  88           88         88          88
      d8YaaaaY8b     88""""""'    88          88  88           88         88          88
     d8""""""""8b    88           Y8,        ,8P  88           88         Y8,        ,8P
    d8'        `8b   88            Y8a.    .a8P   88           88          Y8a.    .a8P
   d8'          `8b  88             `"Y8888Y"'    88888888888  88888888888  `"Y8888Y"'




*/


using Godot;
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
