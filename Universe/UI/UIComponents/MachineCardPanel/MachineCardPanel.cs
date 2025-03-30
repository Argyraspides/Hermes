using System.Collections.Generic;
using Godot;
using Hermes.Core.Machine;
using Hermes.Universe.Autoloads;
using GlobalEventBus = Hermes.Universe.Autoloads.EventBus.GlobalEventBus;

namespace Hermes.Universe.UI.UIComponents.MachineCardPanel;

public partial class MachineCardPanel : Control
{

    List<Machine> machines = new List<Machine>();

    private VBoxContainer m_cardStack;
    private PanelContainer m_panelBackground;
    private Button m_collapsePanelButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEventBus.Instance.MachineEventBus.NewMachineConnected += OnNewMachineConnected;
        GlobalEventBus.Instance.MachineEventBus.MachineDisconnected += OnNewMachineDisconnected;

        m_panelBackground = GetNode<PanelContainer>("PanelBackground");

        m_cardStack = m_panelBackground.GetNode<VBoxContainer>("CardStack");

        m_collapsePanelButton = GetNode<Button>("CollapsePanelButton");

        m_collapsePanelButton.Pressed += OnCollapsePanelButtonPressed;


        // Give panel background minimum size so that it shows up even if there's no vehicles and fills out the
        // entire VehicleCardPanel component
        m_panelBackground.CustomMinimumSize =
            new Vector2(GetViewport().GetWindow().Size.X * 0.25f,
                GetViewport().GetWindow().Size.Y);

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnNewMachineConnected(Machine machine)
    {
        machines.Add(machine);

        var machineCardScene = GD.Load<PackedScene>("res://Universe/UI/UIComponents/MachineCard/MachineCard.tscn");
        var machineCardInstance = machineCardScene.Instantiate<Hermes.Universe.UI.UIComponents.MachineCard.MachineCard>();
        machineCardInstance.Machine = machine;
        m_cardStack.AddChild(machineCardInstance);
    }

    public void OnNewMachineDisconnected(Machine machine)
    {
        machines.Remove(machine);
        foreach (MachineCard.MachineCard machineCard in m_cardStack.GetChildren())
        {
            if (machineCard.Machine == machine)
            {
                machineCard.QueueFree();
            }
        }
    }

    public void OnCollapsePanelButtonPressed()
    {
        m_panelBackground.Visible = !m_panelBackground.Visible;
        m_collapsePanelButton.Text = m_panelBackground.Visible ? "<" : ">";
    }
}
