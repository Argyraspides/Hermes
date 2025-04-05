using Godot;
using Hermes.Core.Machine;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class UIEventBus : Node
{

    [Signal]
    public delegate void MachineCardClickedEventHandler(Machine machine);

    private void OnMachineCardClicked(Machine machine)
    {
        EmitSignal(SignalName.MachineCardClicked, machine);
    }

    public void LoadUINodes()
    {
        LoadMachineCardNode();
    }

    public void ConnectUINodes()
    {
    }

    private void ConnectMachineCardNode()
    {
    }

    private void LoadMachineCardNode()
    {
    }
}
