using System.Collections.Generic;
using Godot;
using Hermes.Common.Map.Utils;
using Hermes.Core.Vehicle.Components;
using Hermes.Core.Vehicle.Components.ComponentStates;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Core.Vehicle;

// Vehicle manager should literally just do these thing:
// - Listen for hellenic message, add vehicle if it didn't exist before
// - Use the component translator to get the right component, or update it
// - Hand the component off to the vehicle
// - Remove vehicles if they become stale

// Nothing else!!
public partial class VehicleManager : Node
{
    public static VehicleManager Instance { get; private set; }

    [Signal]
    public delegate void NewVehicleConnectedEventHandler(Vehicle vehicle);

    private Dictionary<uint, Vehicle> m_Vehicles = new Dictionary<uint, Vehicle>();

    public override void _Ready()
    {
        Instance = this;
        // TODO::ARGYRASPIDES() { Should be done by the event bus }
        ProtocolManager.Instance.HellenicMessageReceived += OnHellenicMessageReceived;
    }

    void UpdateVehicle(HellenicMessage message)
    {
        if (m_Vehicles.TryAdd(message.EntityId, new Vehicle()))
        {
            EmitSignal(SignalName.NewVehicleConnected, m_Vehicles[message.EntityId]);
        }
        ComponentType componentType = HellenicMessageToComponentConverter.GetComponentTypeByMessage(message);
        Vehicle vehicle = m_Vehicles[message.EntityId];
        vehicle.AddComponent(HellenicMessageToComponentConverter.GetComponentByType(componentType));
        vehicle.UpdateComponent(message);
    }

    // Clean up any stale vehicles
    void CleanupVehicles()
    {
        // TODO::ARGYRASPIDES() { Maybe have some timer here for staleness and then just remove from dictionary }
    }

    public void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateVehicle(message);
        CleanupVehicles();
    }
}
