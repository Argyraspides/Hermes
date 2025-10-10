using Godot;
using System;
using System.Collections.Generic;
using Hermes.Core.Autoloads.EventBus;
using Hermes.Core.Machine.Machine;

public partial class TelemetryPanel : PanelContainer
{

    private Machine m_machine; // Machine for which this telemetry panel is for
    private GridContainer m_telemetryPanelGrid; // Has all labels for telemetry panels
    private int m_telemetryPanelColumns = 3;

    private Dictionary<uint, RichTextLabel> m_labels = new Dictionary<uint, RichTextLabel>();

	public override void _Ready()
	{

        HermesEventBus.Instance.UIEventBus.FocussedMachineChanged += OnFocussedMachineChanged;

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


    void LoadTelemetryPanel()
    {
        LoadAltitudeLabel();
        LoadGroundSpeedLabel();
    }

    void LoadAltitudeLabel()
    {
        // Altitude
        RichTextLabel altLabel = new RichTextLabel();
        altLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        altLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        altLabel.BbcodeEnabled = true;
        m_labels[(uint)HellenicMessageType.Altitude] = altLabel;
        m_telemetryPanelGrid.AddChild(altLabel);
    }

    void LoadGroundSpeedLabel()
    {
        // Ground speed
        RichTextLabel gspdLabel = new RichTextLabel();
        gspdLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        gspdLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        gspdLabel.BbcodeEnabled = true;
        m_labels[(uint)HellenicMessageType.GroundVelocity] = gspdLabel;
        m_telemetryPanelGrid.AddChild(gspdLabel);
    }

    private void OnFocussedMachineChanged(Machine machine)
    {
        m_machine = machine;
    }
}
