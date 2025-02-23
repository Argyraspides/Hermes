namespace Hermes.Universe.Autoloads;

using Hermes.Universe.SolarSystem;
using Godot;

public partial class EventBus : Node
{
    public static EventBus Instance { get; set; }

    // ***************** PLANET ORBITAL CAMERA ***************** //

    private PlanetOrbitalCamera m_mainCamera;

    [Signal]
    public delegate void PlanetOrbitalCameraLatLonChangedEventHandler(double latitude, double longitude);

    [Signal]
    public delegate void PlanetOrbitalCameraAltChangedEventHandler(double altitude);

    private void OnPlanetOrbitalCameraLatLonChanged(double latitude, double longitude)
    {
        EmitSignal(SignalName.PlanetOrbitalCameraLatLonChanged, latitude, longitude);
    }

    private void OnPlanetOrbitalCameraAltChanged(double altitude)
    {
        EmitSignal(SignalName.PlanetOrbitalCameraAltChanged, altitude);
    }


    // ***************** INITIALIZERS ***************** //


    public override void _Ready()
    {
        Instance = this;
        GetTree().Root.Ready += OnSceneTreeReady;
    }

    private void OnSceneTreeReady()
    {
        LoadNodes();
        ConnectNodes();
    }

    private void LoadNodes()
    {
        var solarSystem = GetTree().CurrentScene;
        m_mainCamera = solarSystem.GetNode<PlanetOrbitalCamera>("Earth/EarthOrbitalCamera");
    }

    private void ConnectNodes()
    {
        m_mainCamera.OrbitalCameraAltChanged += OnPlanetOrbitalCameraAltChanged;
        m_mainCamera.OrbitalCameraLatLonChanged += OnPlanetOrbitalCameraLatLonChanged;
    }
}
