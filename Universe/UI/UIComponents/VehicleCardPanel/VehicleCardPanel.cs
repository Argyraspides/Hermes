using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle;
using Hermes.Universe.Autoloads;

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
        EventBus.Instance.NewVehicleConnected += OnNewVehicleConnected;

        m_panelBackground = GetNode<PanelContainer>("PanelBackground");

        // TODO::ARGYRASPIDES() { Not sure I like this ... have to keep track of state for minimum size between
        // the card panel and the Vehicle card. I suppose its not a big deal since these two components go hand in hand but
        // eh }
        // Give panel background minimum size so that it shows up even if there's no vehicles and fills out the
        // entire VehicleCardPanel component
        m_panelBackground.CustomMinimumSize =
            new Vector2(GetViewport().GetWindow().Size.X * 0.25f,
                GetViewport().GetWindow().Size.Y);

        m_cardStack = m_panelBackground.GetNode<VBoxContainer>("CardStack");

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
        m_panelBackground.Visible = !m_panelBackground.Visible;
        m_collapsePanelButton.Text = m_panelBackground.Visible ? "<" : ">";
    }
}
