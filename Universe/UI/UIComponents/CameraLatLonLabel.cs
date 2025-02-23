/*




   88888888ba   88        88  88  88888888ba,    88         db         ad88888ba
   88      "8b  88        88  88  88      `"8b   88        d88b       d8"     "8b
   88      ,8P  88        88  88  88        `8b  88       d8'`8b      Y8,
   88aaaaaa8P'  88aaaaaaaa88  88  88         88  88      d8'  `8b     `Y8aaaaa,
   88""""""'    88""""""""88  88  88         88  88     d8YaaaaY8b      `"""""8b,
   88           88        88  88  88         8P  88    d8""""""""8b           `8b
   88           88        88  88  88      .a8P   88   d8'        `8b  Y8a     a8P
   88           88        88  88  88888888Y"'    88  d8'          `8b  "Y88888P"

                                SCULPTOR OF THE STARS

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
