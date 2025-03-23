using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle;
using Hermes.Universe.Autoloads;

namespace Hermes.Universe.UI.UIComponents.VehicleCardPanel;

public partial class VehicleCardPanel : Control
{

    List<Vehicle> vehicles = new List<Vehicle>();

    private VBoxContainer m_cardStack;
    private Button m_collapsePanelButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        EventBus.Instance.NewVehicleConnected += OnNewVehicleConnected;

        m_cardStack = GetNode<VBoxContainer>("CardStack");

        m_collapsePanelButton = GetNode<Button>("CollapsePanelButton");

        m_collapsePanelButton.Pressed += OnCollapsePanelButtonPressed;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnNewVehicleConnected(Vehicle vehicle)
    {
        vehicles.Add(vehicle);

        var vehicleCardScene = GD.Load<PackedScene>("res://Universe/UI/UIComponents/VehicleCard/VehicleCard.tscn");
        var vehicleCardInstance = vehicleCardScene.Instantiate<Hermes.Universe.UI.UIComponents.VehicleCard.VehicleCard>();
        vehicleCardInstance.Vehicle = vehicle;
        m_cardStack.AddChild(vehicleCardInstance);
    }

    public void OnNewVehicleDisconnected(Vehicle vehicle)
    {

    }

    public void OnCollapsePanelButtonPressed()
    {
        m_cardStack.Visible = !m_cardStack.Visible;
        m_collapsePanelButton.Text = m_cardStack.Visible ? "<" : ">";
    }
}
