using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using Hermes.Core.Machine.Machine;


// Supposed to, based on the vehicle type, load the appropriate telemetry.
// Right now just loads basic fundamental properties to different vehicle types
public partial class TelemetryPanel : PanelContainer
{

    private Dictionary<uint, RichTextLabel> m_labels = new Dictionary<uint, RichTextLabel>();

    void LoadTelemetryPanel()
    {
        // Altitude
        RichTextLabel altLabel = new RichTextLabel();
        altLabel.Text = "[b]ALT: - ";
        altLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        altLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        m_labels[(uint)HellenicMessageType.Altitude] = altLabel;
        m_telemetryPanelGrid.AddChild(altLabel);

        // Ground speed
        RichTextLabel gspdLabel = new RichTextLabel();
        gspdLabel.Text = "[b]GSPD: - ";
        gspdLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        gspdLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        m_labels[(uint)HellenicMessageType.GroundVelocity] = gspdLabel;
        m_telemetryPanelGrid.AddChild(gspdLabel);
    }

    void UpdateTelemetryPanel(Machine machine)
    {

        if (machine == null || !machine.MachineId.HasValue)
        {
            ClearTelemetryPanel();
        }

        Dictionary<uint, HellenicMessage> machineState = machine.HellenicMessages;

        UpdateAltitude(machineState);
        UpdateGroundVelocity(machineState);
    }

    void UpdateAltitude(Dictionary<uint, HellenicMessage> machineState)
    {
        if (machineState.TryGetValue((uint)HellenicMessageType.Altitude, out HellenicMessage altMsg))
        {
            m_labels.TryGetValue((uint)HellenicMessageType.Altitude, out RichTextLabel altLabel);
            Altitude alt = (Altitude)altMsg;

            if (!alt.Alt.HasValue)
            {
                return;
            }

            altLabel.Text = $"ALT: {alt.Alt.ToString()}";
        }
    }

    void UpdateGroundVelocity(Dictionary<uint, HellenicMessage> machineState)
    {
        if (machineState.TryGetValue((uint)HellenicMessageType.GroundVelocity, out HellenicMessage grndVel))
        {
            m_labels.TryGetValue((uint)HellenicMessageType.GroundVelocity, out RichTextLabel gspdLabel);
            GroundVelocity gvel = (GroundVelocity)grndVel;


            if (!gvel.Vx.HasValue || !gvel.Vy.HasValue)
            {
                return;
            }

            double gspd = Math.Sqrt((gvel.Vx.Value * gvel.Vx.Value) + (gvel.Vy.Value * gvel.Vy.Value));
            gspdLabel.Text = $"GSPD: {gspd.ToString("F1")}";
        }
    }

    void ClearTelemetryPanel()
    {
        foreach (RichTextLabel label in m_labels.Values)
        {
            label.Text = "";
        }
    }
}
