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
using Hermes.Universe.SolarSystem;


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

        // TODO::ARGYRASPIDES() { Eh ... I'm not so sure I like this. The idea here is that because the event buses
        // always wait for the entire scene tree to be loaded before connecting signals, then if some signals
        // are emitted by some nodes in their _Ready() function, then any other nodes that want to be aware of this
        // signal won't receive them. In this case, as soon as the planet orbital camera enters the scene tree,
        // then it will emit a signal that its lat/lon/alt has changed so that the labels on the bottom of the screen
        // can show the correct initial lat/lon/alt value. However, the label nodes might not have been made yet so the signal is lost.
        // We *can* just have the labels load up first, but thats stupid as it means simply reordering the nodes
        // results in broken signals. That wouldn't work in this case anyways as there are still more nodes to load besides
        // the planet orbital camera and the labels, so even if the labels loaded first, they are connected to the event bus signals
        // which haven't been stitched yet as its still waiting for the rest of the scene tree to load. Emitting them here is a better solution
        // but now our signal bus is starting to have knowledge about what the *purpose* of the signals are in the context of the
        // application rather than simply being a router. If information sharing gets complex then the event buses will become god-mode
        // classes and incredibly hairy, when an event bus is suppose to be a nice and clean routing table. Think of a better solution later }
        m_mainCamera.EmitSignal("OrbitalCameraLatLonChanged", m_mainCamera.DisplayLat, m_mainCamera.DisplayLon);
        m_mainCamera.EmitSignal("OrbitalCameraAltChanged", m_mainCamera.CurrentAltitude);
    }

}
