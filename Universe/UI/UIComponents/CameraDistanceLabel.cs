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
