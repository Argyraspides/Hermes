using Godot;
using System;
using Hermes.Core.Autoloads.EventBus;
using Hermes.Core.Machine.Machine;

public partial class TelemetryPanel : PanelContainer
{

    private Machine m_machine; // Machine for which this telemetry panel is for
    private GridContainer m_telemetryPanelGrid; // Has all labels for telemetry panels
    private int m_telemetryPanelColumns = 5;

	public override void _Ready()
	{

        GlobalEventBus.Instance.UIEventBus.FocussedMachineChanged += OnFocussedMachineChanged;

        m_telemetryPanelGrid = GetNode<GridContainer>("VBoxContainer/TelemetryPanelTelemetryMargin/TelemetryPanelTelemetry");
        m_telemetryPanelGrid.Columns = m_telemetryPanelColumns;

        // In TelemetryPanelLoader.cs
        LoadTelemetryPanel();

	}

	public override void _Process(double delta)
	{
        if (m_machine == null)
        {
            return;
        }

        // In TelemetryPanelLoader.cs
        UpdateTelemetryPanel(m_machine);
	}

    private void OnFocussedMachineChanged(Machine machine)
    {
        m_machine = machine;
    }
}
