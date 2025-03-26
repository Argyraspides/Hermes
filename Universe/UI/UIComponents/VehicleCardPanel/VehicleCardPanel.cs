using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle;
using Hermes.Universe.Autoloads;
using GlobalEventBus = Hermes.Universe.Autoloads.EventBus.GlobalEventBus;

namespace Hermes.Universe.UI.UIComponents.VehicleCardPanel;

public partial class VehicleCardPanel : Control
{

    List<Vehicle> vehicles = new List<Vehicle>();

    private VBoxContainer m_cardStack;
    private PanelContainer m_panelBackground;
    private Button m_collapsePanelButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEventBus.Instance.VehicleEventBus.NewVehicleConnected += OnNewVehicleConnected;
        GlobalEventBus.Instance.VehicleEventBus.VehicleDisconnected += OnNewVehicleDisconnected;

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
        vehicles.Remove(vehicle);
        foreach (VehicleCard.VehicleCard vehicleCard in m_cardStack.GetChildren())
        {
            if (vehicleCard.Vehicle == vehicle)
            {
                vehicleCard.QueueFree();
            }
        }
    }

    public void OnCollapsePanelButtonPressed()
    {
        m_panelBackground.Visible = !m_panelBackground.Visible;
        m_collapsePanelButton.Text = m_panelBackground.Visible ? "<" : ">";
    }
}
