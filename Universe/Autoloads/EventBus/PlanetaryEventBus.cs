using Godot;
using Hermes.Universe.SolarSystem;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class PlanetaryEventBus : Node
{
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

    public void LoadPlanetOrbitalCameraNodes()
    {
        var solarSystem = GetTree().CurrentScene;
        m_mainCamera = solarSystem.GetNode<PlanetOrbitalCamera>("Earth/EarthOrbitalCamera");
    }

    public void ConnectPlanetOrbitalCameraNodes()
    {
        m_mainCamera.OrbitalCameraAltChanged += OnPlanetOrbitalCameraAltChanged;
        m_mainCamera.OrbitalCameraLatLonChanged += OnPlanetOrbitalCameraLatLonChanged;
    }

}
