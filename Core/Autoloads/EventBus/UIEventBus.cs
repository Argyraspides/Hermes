using Godot;
using Hermes.Core.Machine.Machine;

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
    public delegate void MachineSelectedEventHandler(Machine.Machine.Machine machine);
    public void OnMachineClicked(Machine.Machine.Machine machine)
    {
        EmitSignal(SignalName.MachineSelected);
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

    [Signal]
    public delegate void FocussedMachineChangedEventHandler(Machine.Machine.Machine machine);

    public void OnFocussedMachineChanged(Machine.Machine.Machine machine)
    {
        EmitSignal(SignalName.FocussedMachineChanged, machine);
    }


    [Signal]
    public delegate void TakeoffControlClickedEventHandler(bool clickedState);
    public void OnTakeoffControlClicked(bool clickedState)
    {
        EmitSignal(SignalName.TakeoffControlClicked, clickedState);
    }

    [Signal]
    public delegate void LandControlClickedEventHandler(bool clickedState);
    public void OnLandControlClicked(bool clickedState)
    {
        EmitSignal(SignalName.LandControlClicked, clickedState);
    }

}
