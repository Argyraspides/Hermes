using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;
using Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;
using Hermes.Universe.Autoloads.EventBus;
using Hermes.Universe.UI.UIComponents;

public partial class TakeoffControlComponent : HBoxContainer
{

    private HellenicCommander           m_commander;

    private Dictionary<uint, Machine>   m_machines;

    private TextureButton               m_takeoffButton;

    private VSlider                     m_altitudeSlider;

    private LineEdit                    m_maxAltitudeLabel;

    private RichTextLabel               m_currentAltitudeLabel;

    private VBoxContainer               m_takeoffButtonContainer;

    private VBoxContainer               m_altitudeSliderComponent;


    private const double DEFAULT_MAX_TAKEOFF_ALTITUDE = 50;
    private double m_currentMaxTakeoffAltitude = DEFAULT_MAX_TAKEOFF_ALTITUDE;

	public override void _Ready()
	{

        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;
        GlobalEventBus.Instance.UIEventBus.ConfirmationSliderConfirmed += OnConfirmationSliderConfirmed;

        m_takeoffButtonContainer = GetNode<VBoxContainer>("TakeoffButtonContainer");
        m_altitudeSliderComponent = GetNode<VBoxContainer>("AltitudeSliderComponent");

        m_takeoffButton = GetNode<TextureButton> ("TakeoffButtonContainer/TakeoffButton");

        m_altitudeSlider = GetNode<VSlider> ("AltitudeSliderComponent/SliderCenterContainer/AltitudeSlider");
        m_altitudeSlider.MaxValue = DEFAULT_MAX_TAKEOFF_ALTITUDE;
        m_altitudeSlider.ValueChanged += OnAltitudeSliderSliding;

        m_currentAltitudeLabel = GetNode<RichTextLabel> ("AltitudeSliderComponent/CurrentAltLabel");

        m_maxAltitudeLabel = GetNode<LineEdit> ("AltitudeSliderComponent/MaxAltLabel");
        m_maxAltitudeLabel.TextSubmitted += OnMaxAltitudeUpdated;
        m_maxAltitudeLabel.FocusExited += () => { OnMaxAltitudeUpdated(m_maxAltitudeLabel.Text); };

        m_commander = new HellenicCommander();
    }
    public override void _ExitTree()
    {
        GlobalEventBus.Instance.UIEventBus.MachineCardClicked -= OnMachineCardClicked;
        m_commander.Dispose();
    }
    public void SetMachines(Dictionary<uint, Machine> machines)
    {
        m_machines = new Dictionary<uint, Machine>(machines);
    }
    private void OnAltitudeSliderSliding(double val)
    {
        m_currentAltitudeLabel.Text = $"[center]{val}m";
    }
    private void OnMaxAltitudeUpdated(string s)
    {
        /* TODO::ARGYRASPIDES() {
                think about how you're going to standardize units ... right now everything is hardcoded to SI units
            }
        */
        if (string.IsNullOrEmpty(s))
        {
            m_maxAltitudeLabel.Text = $"{DEFAULT_MAX_TAKEOFF_ALTITUDE}m";
            return;
        }

        try
        {
            m_currentMaxTakeoffAltitude = Convert.ToDouble(m_maxAltitudeLabel.Text.Replace("m", ""));
            if (s.Last() != 'm') m_maxAltitudeLabel.Text += 'm';

            m_altitudeSlider.MaxValue = m_currentMaxTakeoffAltitude;
        }
        catch (FormatException _) // Not a valid double
        {
            m_maxAltitudeLabel.Text = $"{DEFAULT_MAX_TAKEOFF_ALTITUDE}m";
        }

    }
    private void SetMachineIcon()
    {
        string normalIconPath = "res://Universe/UI/Assets/TakeoffIcon2.png";
        string pressedIconPath = "res://Universe/UI/Assets/TakeoffIcon2Pressed.png";

        Machine machine;
        IEnumerable<Machine> allMachines = m_machines.Values.Distinct();

        if (allMachines.Count() == 1)
        {
            machine = allMachines.First();
            normalIconPath = machine.MachineType switch
            {
                MachineType.Quadcopter => "res://Universe/UI/Assets/TakeoffQuadcopter.png",
                _ => "res://Universe/UI/Assets/TakeoffIcon2.png"
            };

            pressedIconPath = machine.MachineType switch
            {
                MachineType.Quadcopter => "res://Universe/UI/Assets/TakeoffQuadcopterSelected.png",
                _ => "res://Universe/UI/Assets/TakeoffIcon2Pressed.png"
            };
        }

        m_takeoffButton.TextureNormal = GD.Load<Texture2D>(normalIconPath);
        m_takeoffButton.TexturePressed = GD.Load<Texture2D>(pressedIconPath);

    }
    private void OnMachineCardClicked(Machine machine)
    {
        if (!HermesUtils.IsValid(machine) || !machine.MachineId.HasValue || m_machines == null)
        {
            return;
        }

        if (m_machines.ContainsKey(machine.MachineId.Value))
        {
            m_machines.Remove(machine.MachineId.Value);
        }
        else
        {
            m_machines.Add(machine.MachineId.Value, machine);
        }

        SetMachineIcon();

    }
    private void OnConfirmationSliderConfirmed()
    {
        /* TODO::ARGYRASPIDES() {
         *      Make a filter here based on what machines we have? We should not hardcode a quadcopter ...
         *  }
         */
        if (!m_takeoffButton.IsPressed()) return;

        foreach (Machine machine in m_machines.Values)
        {
            m_commander.TakeoffQuadcopter(machine, m_altitudeSlider.Value);
        }

        m_takeoffButton.SetPressed(false);

    }

}
