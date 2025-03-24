/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


namespace Hermes.Universe.Autoloads;

using Hermes.Core.Vehicle;
using Hermes.Languages.HellenicGateway;
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
        m_vehicleManager = new VehicleManager();
        AddChild(m_vehicleManager);
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
        EmitSignal(SignalName.HellenicMessageReceived, message);
    }

    private void LoadProtocolManagerNode()
    {
        m_protocolManager = new ProtocolManager();
        AddChild(m_protocolManager);
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
