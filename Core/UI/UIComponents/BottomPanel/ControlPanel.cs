using System.Collections.Generic;
using System.Linq;
using Godot;
using Hermes.Common.HermesUtils;
using Hermes.Core.Autoloads.EventBus;
using Hermes.Core.Machine.CapabilityEngine;
using Hermes.Core.Machine.Machine;

/*
 * TODO::ARGYRASPIDES() {
 *  when doing like screen resizes down to 720p, the control panel can literally only fit like two buttons. This is kind of a problem,
 *  though i'll have to test out how screen scaling and stuff works. On my screen at least this means at lower resolutions its gonna be a problem
 *  but we'll see. Think about this later
 *  }
 *   TODO::ARGYRASPIDES() {
 *   Make sure to explicitly set the colors and stuff for the different components using the UI constants ...
 *   though in future idk how sustainable this is, as the godot editor is super powerful for rapid iteration. Well I guess I could use
 *   Godot editor for tinkering, use the "run specific scene" feature for rapid iteration and then settle on something final by
 *   having it all defined in C#. Think about this later as well
 *   }
 */

namespace Hermes.Core.UI.UIComponents.BottomPanel;

public partial class ControlPanel : PanelContainer
{

    private Dictionary<uint, Machine.Machine.Machine> m_machines = new Dictionary<uint, Machine.Machine.Machine>();

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

    private void OnMachineCardClicked(Machine.Machine.Machine machine)
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

        IEnumerable<Capability> sharedCapabilities = new List<Capability>(m_machines.First().Value.GetCapabilities());

        foreach (KeyValuePair<uint, Machine.Machine.Machine> machine in m_machines)
        {
            IEnumerable<Capability> capabilities = machine.Value.GetCapabilities();

            // If a machine has zero capabilities then no selected vehicle has any capabilities common with
            // everyone else
            if (capabilities.Count() == 0)
            {
                UnloadAllControlComponents();
                return;
            }
            IEnumerable<Capability> shared =
                capabilities.Where(
                    capability => sharedCapabilities.Contains(capability));

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
                // TODO:: these should be like generic not specific to a multicopter?
                case Capability.MULTIROTOR_TAKEOFF:
                    LoadTakeoffComponent();
                    break;
                case Capability.MULTIROTOR_LANDING:
                    LoadLandComponent();
                    break;
            }
        }

    }

    private void LoadTakeoffComponent()
    {
        var takeoffComponent =
            GD.Load<PackedScene>("res://Core/UI/UIComponents/MachineControl/TakeoffControlComponent/TakeoffControlComponent.tscn");

        MachineControl.TakeoffControlComponent.TakeoffControlComponent takeoffComponentInstance =
            takeoffComponent.Instantiate<MachineControl.TakeoffControlComponent.TakeoffControlComponent>();

        takeoffComponentInstance.SetMachines(m_machines);
        takeoffComponentInstance.Name = "TakeoffControlComponent";

        m_controlPanelBar.AddChild(takeoffComponentInstance);
    }

    private void LoadLandComponent()
    {
        var landComponent =
            GD.Load<PackedScene>("res://Core/UI/UIComponents/MachineControl/LandControlComponent/LandControlComponent.tscn");

        MachineControl.LandControlComponent.LandControlComponent landControlComponentInstance =
            landComponent.Instantiate<MachineControl.LandControlComponent.LandControlComponent>();

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
