using Godot;
using Hermes.Core.Autoloads.EventBus;

namespace Hermes.Core.UI.UIComponents.MachineControl.ConfirmationSlider;

public partial class ConfirmationSlider : MarginContainer
{

    [Signal]
    public delegate void ConfirmationSliderConfirmedEventHandler();

    private HSlider m_confirmationSlider;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        m_confirmationSlider = GetNode<HSlider>("HSlider");
        m_confirmationSlider.DragEnded += OnSliderDragged;

        ConfirmationSliderConfirmed += HermesEventBus.Instance.UIEventBus.OnConfirmationSliderConfirmed;

    }

    private void OnSliderDragged(bool valueChanged)
    {
        if (m_confirmationSlider.Value.Equals(m_confirmationSlider.MaxValue))
        {
            EmitSignal(SignalName.ConfirmationSliderConfirmed);
        }
        m_confirmationSlider.Value = m_confirmationSlider.MinValue;
    }
}
