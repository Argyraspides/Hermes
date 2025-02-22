using Godot;
using Hermes.Universe.SolarSystem;
using Hermes.Universe.SolarSystem.Planets.Earth;

// TODO(Argyraspides, 16/02/2025): This is implemented absolutely horribly. Just for testing for now
public partial class MainUI : Control
{
    // Called when the node enters the scene tree for the first time.
    private HBoxContainer m_cameraInfoBar;
    private RichTextLabel m_cameraDistanceLabel;

    private Earth m_earth;
    private PlanetOrbitalCamera m_cameraOrbitalCamera;

    public override void _Ready()
    {
        m_earth = GetParent().GetNode<Earth>("Earth");
        m_cameraInfoBar = GetNode<HBoxContainer>("CameraInfoBar");
        m_cameraDistanceLabel = m_cameraInfoBar.GetNode<RichTextLabel>("CameraDistanceLabel");

        m_cameraOrbitalCamera = m_earth.GetNode<PlanetOrbitalCamera>("EarthOrbitalCamera");
        m_cameraOrbitalCamera.OrbitalCameraPosChanged += OnCameraPositionChanged;
    }

    public void OnCameraPositionChanged(Vector3 position, float latitude, float longitude)
    {
        UpdateCameraInfoText(position, latitude, longitude);
    }

    public void UpdateCameraInfoText(Vector3 cameraPosition, float latitude, float longitude)
    {
        float trueDist = m_earth.GlobalPosition.DistanceTo(cameraPosition);
        trueDist -= SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;

        latitude = Mathf.RadToDeg(latitude);
        longitude = Mathf.RadToDeg(longitude);

        m_cameraDistanceLabel.Text =
            "[center][b]" +
            trueDist.ToString("F3") +
            " km\n" +
            latitude.ToString("F6") + "\u00b0, " + longitude.ToString("F6") + "\u00b0";
    }
}
