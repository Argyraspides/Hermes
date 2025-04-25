using Godot;
using System;

public partial class BottomPanel : VBoxContainer
{
    private Button m_collapsePanelButton;
    private HBoxContainer m_controlAndTelemetryPanels;

	public override void _Ready()
	{
        m_collapsePanelButton = GetNode<Button>("CollapsePanelButton");
        m_collapsePanelButton.Pressed += OnCollapsePanelButtonPressed;

        m_controlAndTelemetryPanels = GetNode<HBoxContainer>("ControlAndTelemetryPanels");

    }

    private void OnCollapsePanelButtonPressed()
    {
        m_controlAndTelemetryPanels.Visible = !m_controlAndTelemetryPanels.Visible;
        m_collapsePanelButton.Text = m_controlAndTelemetryPanels.Visible ? "\u25bc" : "\u25b2";
    }
}
