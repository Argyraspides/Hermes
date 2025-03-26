/*




   88888888ba   88        88  88  88888888ba,    88         db         ad88888ba
   88      "8b  88        88  88  88      `"8b   88        d88b       d8"     "8b
   88      ,8P  88        88  88  88        `8b  88       d8'`8b      Y8,
   88aaaaaa8P'  88aaaaaaaa88  88  88         88  88      d8'  `8b     `Y8aaaaa,
   88""""""'    88""""""""88  88  88         88  88     d8YaaaaY8b      `"""""8b,
   88           88        88  88  88         8P  88    d8""""""""8b           `8b
   88           88        88  88  88      .a8P   88   d8'        `8b  Y8a     a8P
   88           88        88  88  88888888Y"'    88  d8'          `8b  "Y88888P"

                                PAINTER OF PARADISE

*/


using Hermes.Universe.Autoloads.EventBus;

namespace Hermes.Universe.UI.UIComponents;

using Godot;
using Hermes.Universe.Autoloads;

public partial class CameraDistanceLabel : RichTextLabel
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
       GlobalEventBus.Instance.PlanetaryEventBus.PlanetOrbitalCameraAltChanged += UpdateCameraAltitudeLabel;
    }

    private void UpdateCameraAltitudeLabel(double altitude)
    {
        Text = "Camera: " + altitude.ToString("F3") + " km";
    }
}
