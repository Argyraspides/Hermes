using Godot;

namespace Hermes.Core.Autoloads.EventBus;

public partial class UIEventBus : Node
{
    [Signal]
    public delegate void MachineCardClickedEventHandler(Machine.Machine.Machine machine);

    public void OnMachineCardClicked(Machine.Machine.Machine machine)
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


    [Signal]
    public delegate void ConfirmationSliderConfirmedEventHandler();
    public void OnConfirmationSliderConfirmed()
    {
        EmitSignal(SignalName.ConfirmationSliderConfirmed);
    }

}
