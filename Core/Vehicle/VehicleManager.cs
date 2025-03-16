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
    private Dictionary<uint, Vehicle> m_Vehicles = new Dictionary<uint, Vehicle>();

    public override void _Ready()
    {
        ProtocolManager.Instance.HellenicMessageReceived += OnHellenicMessageReceived;
    }

    // Update the vehicles components with the message
    void UpdateVehicle(HellenicMessage message)
    {
        m_Vehicles.TryAdd(message.EntityId, new Vehicle());

        Vehicle vehicle = m_Vehicles[message.EntityId];
        ComponentType componentType = HellenicMessageToComponentConverter.GetComponentTypeByMessage(message);
        if (!vehicle.HasComponent(componentType))
        {
            Component newComponent = HellenicMessageToComponentConverter.GetComponentByType(componentType);
            vehicle.AddComponent(newComponent);
        }

        vehicle.UpdateComponent(componentType, message);
    }

    // Clean up any stale vehicles
    void CleanupVehicles()
    {
        // TODO::ARGYRASPIDES() { Maybe have some timer here for staleness and then just remove from dictionary }
    }

    void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateVehicle(message);
        CleanupVehicles();
    }
}
