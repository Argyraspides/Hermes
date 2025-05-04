using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine.Machine;
using Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;
using Hermes.Universe.Autoloads.EventBus;
using Hermes.Universe.UI.UIComponents;

public partial class LandControlComponent : HBoxContainer
{
    private HellenicCommander m_commander;

    private Dictionary<uint, Machine> m_machines;

    private TextureButton m_landButton;

    private VBoxContainer m_landButtonContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;
        GlobalEventBus.Instance.UIEventBus.ConfirmationSliderConfirmed += OnConfirmationSliderConfirmed;

        m_landButton = GetNode<TextureButton> ("LandButtonContainer/LandButton");

        m_landButtonContainer = GetNode<VBoxContainer>("LandButtonContainer");
        m_landButtonContainer.CustomMinimumSize = new Vector2(100, UIConstants.CONTROL_PANEL_MAX_HEIGHT);

        m_commander = new HellenicCommander();
    }

    public override void _ExitTree()
    {
        GlobalEventBus.Instance.UIEventBus.MachineCardClicked -= OnMachineCardClicked;
        m_commander.Dispose();
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

        m_landButton.TextureNormal = GD.Load<Texture2D>(normalIconPath);
        m_landButton.TexturePressed = GD.Load<Texture2D>(pressedIconPath);

    }

    private void OnConfirmationSliderConfirmed()
    {
        /* TODO::ARGYRASPIDES() {
         *      Make a filter here based on what machines we have? We should not hardcode a quadcopter ...
         *  }
         */
        if (!m_landButton.IsPressed()) return;

        foreach (Machine machine in m_machines.Values)
        {
            m_commander.LandQuadcopter(machine);
        }

        m_landButton.SetPressed(false);

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

    public void SetMachines(Dictionary<uint, Machine> machines)
    {
        m_machines = new Dictionary<uint, Machine>(machines);
    }
}
