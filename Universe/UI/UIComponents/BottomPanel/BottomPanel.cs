using Godot;
using System;

public partial class BottomPanel : VBoxContainer
{
    private Button m_collapsePanelButton;
    private HBoxContainer m_controlAndTelemetryPanels;

    private const string UP_TRIANGLE = "\u25b2";
    private const string DOWN_TRIANGLE = "\u25bc";

	public override void _Ready()
	{
        m_collapsePanelButton = GetNode<Button>("CollapsePanelButton");
        m_collapsePanelButton.Pressed += OnCollapsePanelButtonPressed;

        m_controlAndTelemetryPanels = GetNode<HBoxContainer>("ControlAndTelemetryPanels");

    }

    private void OnCollapsePanelButtonPressed()
    {
        m_controlAndTelemetryPanels.Visible = !m_controlAndTelemetryPanels.Visible;
        m_collapsePanelButton.Text = m_controlAndTelemetryPanels.Visible ? DOWN_TRIANGLE : UP_TRIANGLE;
    }
}
