using Hermes.Core.Vehicle;
using Hermes.Languages.HellenicGateway;

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
/// TODO::ARGYRASPIDES() { Split up the event bus into multiple parts so its not one massive file, but separate files for
/// globalizing events for different parts of Hermes }
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

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
        // TODO::ARGYRASPIDES() { Make protocol manager a non-singleton in future, and
        // create a "manager registration" phase where these managers are loaded up, and their
        // events routed to the event bus }
        m_vehicleManager = VehicleManager.Instance;
    }

    private void ConnectVehicleManagerNode()
    {
        m_vehicleManager.NewVehicleConnected += OnNewVehicleConnected;
    }


    // ***************** PROTOCOL MANAGER ***************** //

    private ProtocolManager m_protocolManager;

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        EmitSignal(SignalName.HellenicMessageReceived);
    }

    private void LoadProtocolManagerNode()
    {
        // TODO::ARGYRASPIDES() { Make protocol manager a non-singleton in future, and
        // create a "manager registration" phase where these managers are loaded up, and their
        // events routed to the event bus }
        m_protocolManager = ProtocolManager.Instance;
    }

    private void ConnectProtocolManagerNode()
    {
        m_protocolManager.HellenicMessageReceived += OnHellenicMessageReceived;
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
        LoadProtocolManagerNode();
    }

    private void ConnectNodes()
    {
        ConnectPlanetOrbitalCameraNodes();
        ConnectVehicleManagerNode();
        ConnectProtocolManagerNode();
    }
}
