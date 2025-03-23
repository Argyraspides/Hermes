using Hermes.Core.Vehicle;

namespace Hermes.Universe.Autoloads;

using Hermes.Universe.SolarSystem;
using Godot;

/// <summary>
/// The event bus is mainly used to inform UI components about changes that have occurred in Hermes' backend.
/// The reason for this is that the UI layer's subtree is very distant from the rest of Hermes', making communication
/// between these distant components and the UI a bit awkward to do in a clean way.
///
/// See: https://www.gdquest.com/tutorial/godot/design-patterns/event-bus-singleton/
///
/// It is important that any signals that *are* routed through the event bus are kept to a minimum as to avoid
/// a barrage of signal calls being routed through one place.
/// </summary>
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

    private void LoadPlanetOrbitalCameraNodes()
    {
        var solarSystem = GetTree().CurrentScene;
        m_mainCamera = solarSystem.GetNode<PlanetOrbitalCamera>("Earth/EarthOrbitalCamera");
    }

    private void ConnectPlanetOrbitalCameraNodes()
    {
        m_mainCamera.OrbitalCameraAltChanged += OnPlanetOrbitalCameraAltChanged;
        m_mainCamera.OrbitalCameraLatLonChanged += OnPlanetOrbitalCameraLatLonChanged;
    }

    // ***************** VEHICLE MANAGER ***************** //

    private VehicleManager m_vehicleManager;

    [Signal]
    public delegate void NewVehicleConnectedEventHandler(Vehicle vehicle);

    [Signal]
    public delegate void NewVehicleDisconnectedEventHandler(Vehicle vehicle);

    private void OnNewVehicleConnected(Vehicle vehicle)
    {
        EmitSignal(SignalName.NewVehicleConnected, vehicle);
    }

    private void OnNewVehicleDisconnected(Vehicle vehicle)
    {
        EmitSignal(SignalName.NewVehicleDisconnected, vehicle);
    }

    private void LoadVehicleManagerNode()
    {
        m_vehicleManager = VehicleManager.Instance;
    }

    private void ConnectVehicleManagerNode()
    {
        m_vehicleManager.NewVehicleConnected += OnNewVehicleConnected;
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
        LoadPlanetOrbitalCameraNodes();
        LoadVehicleManagerNode();
    }

    private void ConnectNodes()
    {
        ConnectPlanetOrbitalCameraNodes();
        ConnectVehicleManagerNode();
    }
}
