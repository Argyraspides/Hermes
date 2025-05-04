using Godot;
using System;
using Hermes.Common.HermesUtils;
using Hermes.Universe.Autoloads.EventBus;

public partial class ConfirmationSlider : VBoxContainer
{

    [Signal]
    public delegate void ConfirmationSliderConfirmedEventHandler();

    private HSlider m_confirmationSlider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

        m_confirmationSlider = GetNode<HSlider>("MarginContainer/HSlider");
        m_confirmationSlider.DragEnded += OnSliderDragged;

        ConfirmationSliderConfirmed += GlobalEventBus.Instance.UIEventBus.OnConfirmationSliderConfirmed;

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
