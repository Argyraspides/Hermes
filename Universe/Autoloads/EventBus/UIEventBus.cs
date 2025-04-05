using Godot;
using Hermes.Core.Machine;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class UIEventBus : Node
{
    [Signal]
    public delegate void MachineCardClickedEventHandler(Machine machine);

    public void OnMachineCardClicked(Machine machine)
    {
        EmitSignal(SignalName.MachineCardClicked, machine);
    }
}
