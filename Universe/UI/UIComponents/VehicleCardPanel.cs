using Godot;
using System;
using System.Collections.Generic;
using Hermes.Core.Vehicle;
using Hermes.Universe.Autoloads;

public partial class VehicleCardPanel : Control
{

    List<Vehicle> vehicles = new List<Vehicle>();

    private VBoxContainer m_cardStack;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        EventBus.Instance.NewVehicleConnected += OnNewVehicleConnected;
        m_cardStack = GetNode<VBoxContainer>("CardStack");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
    }

    public void OnNewVehicleConnected(Vehicle vehicle)
    {
        vehicles.Add(vehicle);

        var vehicleCardScene = GD.Load<PackedScene>("res://Universe/UI/UIComponents/VehicleCard.tscn");
        var vehicleCardInstance = vehicleCardScene.Instantiate<VehicleCard>();
        vehicleCardInstance.Vehicle = vehicle;
        m_cardStack.AddChild(vehicleCardInstance);
    }

    public void OnNewVehicleDisconnected(Vehicle vehicle)
    {

    }
}
