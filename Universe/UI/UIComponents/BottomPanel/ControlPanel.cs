using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;
using Hermes.Core.Machine.Machine.Capabilities;
using Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;
using Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;
using Hermes.Universe.Autoloads.EventBus;

public partial class ControlPanel : PanelContainer
{

    private Dictionary<uint, Machine> m_machines = new Dictionary<uint, Machine>();

    private MarginContainer m_controlPanelContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;

        m_controlPanelContainer =
            GetNode<MarginContainer>(
                "VBoxContainer/ControlPanelControls/ControlPanelMarginContainer");
    }

	public override void _Process(double delta)
	{
	}

    private void OnMachineCardClicked(Machine machine)
    {

        if (!HermesUtils.IsValid(machine) || !machine.MachineId.HasValue)
        {
            return;
        }

        if (m_machines.ContainsKey(machine.MachineId.Value))
        {
            m_machines.Remove(machine.MachineId.Value);
        }
        else
        {
            m_machines[machine.MachineId.Value] = machine;
        }

        RefreshControlComponents();

    }

    private void RefreshControlComponents()
    {

        if (m_machines.Count == 0)
        {
            UnloadAllControlComponents();
            return;
        }

        IEnumerable<Capability> sharedCapabilities = new List<Capability>(MachineUtils.GetCapabilities(m_machines.First().Value));

        foreach (var machine in m_machines)
        {
            IEnumerable<Capability> capabilities = MachineUtils.GetCapabilities(machine.Value);
            IEnumerable<Capability> shared =
                capabilities.Where(capability => sharedCapabilities.Contains(capability));

            sharedCapabilities = sharedCapabilities.Concat(shared);
        }

        sharedCapabilities = sharedCapabilities.Distinct();

        if (sharedCapabilities.Count() == 0)
        {
            UnloadAllControlComponents();
        }

        foreach (Capability capability in sharedCapabilities)
        {
            switch (capability)
            {
                case Capability.Takeoff:
                    LoadTakeoffComponent();
                    break;
            }
        }

    }

    private void LoadTakeoffComponent()
    {
        var takeoffComponent =
            GD.Load<PackedScene>("res://Universe/UI/UIComponents/MachineControl/TakeoffControlComponent.tscn");

        TakeoffControlComponent takeoffComponentInstance =
            takeoffComponent.Instantiate<TakeoffControlComponent>();

        takeoffComponentInstance.SetMachines(m_machines);
        takeoffComponentInstance.Name = "TakeoffControlComponent";

        m_controlPanelContainer.AddChild(takeoffComponentInstance);
    }

    private void UnloadAllControlComponents()
    {
        var children = m_controlPanelContainer.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
    }

}
