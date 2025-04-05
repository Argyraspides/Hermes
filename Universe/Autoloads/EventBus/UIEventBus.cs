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


    [Signal]
    public delegate void ZoomInButtonClickedEventHandler();

    public void OnZoomInButtonClicked()
    {
        EmitSignal(SignalName.ZoomInButtonClicked);
    }

    [Signal]
    public delegate void ZoomOutButtonClickedEventHandler();

    public void OnZoomOutButtonClicked()
    {
        EmitSignal(SignalName.ZoomOutButtonClicked);
    }

}
