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


using Hermes.Core.Machine.Machine;

namespace Hermes.Universe.Autoloads.EventBus;

using Godot;
using Hermes.Core.Machine;


public partial class MachineEventBus : Node
{

    private MachineManager m_MachineManager;

    [Signal]
    public delegate void NewMachineConnectedEventHandler(Machine machine);

    [Signal]
    public delegate void MachineDisconnectedEventHandler(Machine machine);

    public void OnNewMachineConnected(Machine machine)
    {
        EmitSignal(SignalName.NewMachineConnected, machine);
    }

    public void OnMachineDisconnected(Machine machine)
    {
        EmitSignal(SignalName.MachineDisconnected, machine);
    }
}
