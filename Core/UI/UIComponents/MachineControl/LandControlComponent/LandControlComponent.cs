using System.Collections.Generic;
using System.Linq;
using Daedalus.GodotUtils;
using Godot;
using Hermes.Core.Autoloads.EventBus;
using Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;

namespace Hermes.Core.UI.UIComponents.MachineControl.LandControlComponent;

public partial class LandControlComponent : HBoxContainer
{

    [Signal]
    public delegate void LandControlClickedEventHandler(bool clickedState);

    private HellenicCommander           m_commander;

    private Dictionary<uint, Machine.Machine.Machine>   m_machines;

    private TextureButton               m_landButton;

    private VBoxContainer               m_landButtonContainer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        HermesEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;
        HermesEventBus.Instance.UIEventBus.ConfirmationSliderConfirmed += OnConfirmationSliderConfirmed;

        LandControlClicked += HermesEventBus.Instance.UIEventBus.OnLandControlClicked;

        m_landButton = GetNode<TextureButton> ("LandButtonContainer/LandButton");
        m_landButton.Pressed += OnLandButtonPressed;

        m_landButtonContainer = GetNode<VBoxContainer>("LandButtonContainer");

        m_commander = new HellenicCommander();
    }
    public override void _ExitTree()
    {
        HermesEventBus.Instance.UIEventBus.MachineCardClicked -= OnMachineCardClicked;
        m_commander.Dispose();
    }
    public void SetMachines(Dictionary<uint, Machine.Machine.Machine> machines)
    {
        m_machines = new Dictionary<uint, Machine.Machine.Machine>(machines);
    }

    private void SetMachineIcon()
    {
        string normalIconPath = "res://Core/UI/Assets/TakeoffIcon2.png";
        string pressedIconPath = "res://Core/UI/Assets/TakeoffIcon2Pressed.png";

        Machine.Machine.Machine machine;
        IEnumerable<Machine.Machine.Machine> allMachines = m_machines.Values.Distinct();

        if (allMachines.Count() == 1)
        {
            machine = allMachines.First();
            normalIconPath = machine.MachineType switch
            {
                MachineType.Quadcopter => "res://Core/UI/Assets/TakeoffQuadcopter.png",
                _ => "res://Core/UI/Assets/TakeoffIcon2.png"
            };

            pressedIconPath = machine.MachineType switch
            {
                MachineType.Quadcopter => "res://Core/UI/Assets/TakeoffQuadcopterSelected.png",
                _ => "res://Core/UI/Assets/TakeoffIcon2Pressed.png"
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

        foreach (Machine.Machine.Machine machine in m_machines.Values)
        {
            m_commander.LandQuadcopter(machine);
        }

        m_landButton.SetPressed(false);
        EmitSignal(SignalName.LandControlClicked, m_landButton.ButtonPressed);

    }
    private void OnMachineCardClicked(Machine.Machine.Machine machine)
    {
        if (!GodotUtils.IsValid(machine) || !machine.MachineId.HasValue || m_machines == null)
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

    private void OnLandButtonPressed()
    {
        EmitSignal(SignalName.LandControlClicked, m_landButton.ButtonPressed);
    }

}
