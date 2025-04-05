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
    // TODO::ARGYRASPIDES() { It seems scroll events leak through the scroll container for some stupid reason, despite everything stopping it up to the
    // main UI. Fix it! Not a criticl issue for now }
    private ScrollContainer m_panelBackground;
    private Button m_collapsePanelButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEventBus.Instance.MachineEventBus.NewMachineConnected += OnNewMachineConnected;
        GlobalEventBus.Instance.MachineEventBus.MachineDisconnected += OnNewMachineDisconnected;

        m_panelBackground = GetNode<ScrollContainer>("PanelBackground");

        m_cardStack = m_panelBackground.GetNode<VBoxContainer>("CardStack");

        m_collapsePanelButton = GetNode<Button>("CollapsePanelButton");

        m_collapsePanelButton.Pressed += OnCollapsePanelButtonPressed;
    }

    public void OnNewMachineConnected(Machine machine)
    {
        machines.Add(machine);

        var machineCardScene = GD.Load<PackedScene>("res://Universe/UI/UIComponents/MachineCard/MachineCard.tscn");
        var machineCardInstance = machineCardScene.Instantiate<Hermes.Universe.UI.UIComponents.MachineCard.MachineCard>();
        machineCardInstance.Machine = machine;
        machineCardInstance.Name = $"MachineCard_{machine.Name}";
        m_cardStack.AddChild(machineCardInstance);
    }

    public void OnNewMachineDisconnected(Machine machine)
    {
        machines.Remove(machine);
        foreach (var child in m_cardStack.GetChildren())
        {
            if (child is MachineCard.MachineCard machineCard && machineCard.Machine == machine)
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
