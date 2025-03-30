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


namespace Hermes.Universe.Autoloads.EventBus;

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
public partial class GlobalEventBus : Node
{
    public static GlobalEventBus Instance { get; private set; }


    public PlanetaryEventBus PlanetaryEventBus { get; private set; }
    public ProtocolEventBus ProtocolEventBus { get; private set; }
    public MachineEventBus MachineEventBus { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        PlanetaryEventBus = new PlanetaryEventBus();
        PlanetaryEventBus.Name = "PlanetaryEventBus";
        AddChild(PlanetaryEventBus);

        ProtocolEventBus = new ProtocolEventBus();
        ProtocolEventBus.Name = "ProtocolEventBus";
        AddChild(ProtocolEventBus);

        MachineEventBus = new MachineEventBus();
        MachineEventBus.Name = "MachineEventBus";
        AddChild(MachineEventBus);

        // Important: Only load and connect the nodes once the main scene tree is ready.
        // Autoloads always load before the scene tree, and some things, such as the planet
        // orbital camera, we want to connect *after* they have loaded into the scene.
        GetTree().Root.Ready += OnSceneTreeReady;
    }

    private void OnSceneTreeReady()
    {
        LoadNodes();
        ConnectNodes();
    }

    private void LoadNodes()
    {
        PlanetaryEventBus.LoadPlanetOrbitalCameraNodes();
        ProtocolEventBus.LoadProtocolManagerNode();
        MachineEventBus.LoadMachineManagerNode();
    }

    private void ConnectNodes()
    {
        PlanetaryEventBus.ConnectPlanetOrbitalCameraNodes();
        ProtocolEventBus.ConnectProtocolManagerNode();
        MachineEventBus.ConnectMachineManagerNode();
    }
}
