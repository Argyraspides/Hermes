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
using Hermes.Universe.UI.UIComponents;

public partial class ControlPanel : PanelContainer
{

    private Dictionary<uint, Machine> m_machines = new Dictionary<uint, Machine>();

    private HBoxContainer m_controlPanelBar;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;

        m_controlPanelBar =
            GetNode<HBoxContainer>(
                "VBoxContainer/ControlPanelControls/ControlPanelMarginContainer/ControlPanelBar");
        m_controlPanelBar.CustomMinimumSize = new Vector2(
                m_controlPanelBar.CustomMinimumSize.X,
                UIConstants.CONTROL_PANEL_MAX_HEIGHT
            );

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

            // If a machine has zero capabilities then no selected vehicle has any capabilities common with
            // everyone else
            if (shared.Count() == 0)
            {
                UnloadAllControlComponents();
                return;
            }

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
                case Capability.Landing:
                    LoadLandComponent();
                    break;
            }
        }

    }

    private void LoadTakeoffComponent()
    {
        var takeoffComponent =
            GD.Load<PackedScene>("res://Universe/UI/UIComponents/MachineControl/TakeoffControlComponent/TakeoffControlComponent.tscn");

        TakeoffControlComponent takeoffComponentInstance =
            takeoffComponent.Instantiate<TakeoffControlComponent>();

        takeoffComponentInstance.SetMachines(m_machines);
        takeoffComponentInstance.Name = "TakeoffControlComponent";

        m_controlPanelBar.AddChild(takeoffComponentInstance);
    }

    private void LoadLandComponent()
    {
        var landComponent =
            GD.Load<PackedScene>("res://Universe/UI/UIComponents/MachineControl/LandControlComponent/LandControlComponent.tscn");

        LandControlComponent landControlComponentInstance =
            landComponent.Instantiate<LandControlComponent>();

        landControlComponentInstance.SetMachines(m_machines);
        landControlComponentInstance.Name = "TakeoffControlComponent";

        m_controlPanelBar.AddChild(landControlComponentInstance);
    }

    private void UnloadAllControlComponents()
    {
        var children = m_controlPanelBar.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
    }

}
