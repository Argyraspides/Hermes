using System.Collections.Generic;
using Godot;
using Hermes.Common.Map.Utils;
using Hermes.Core.Vehicle.Components;
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

    // Add vehicle to dict if its new
    void TryAddVehicle(HellenicMessage message)
    {
        if (m_Vehicles.ContainsKey(message.VehicleId))
        {
            return;
        }

        m_Vehicles.Add(message.VehicleId, new Vehicle());
    }

    // Update the vehicles components with the message
    void UpdateVehicle(HellenicMessage message)
    {
        if (!m_Vehicles.ContainsKey(message.VehicleId))
        {
            return;
        }

        Vehicle vehicle = m_Vehicles[message.VehicleId];

        ComponentType componentType = HellenicMessageToComponentConverter.GetComponentType(message);

        if (!vehicle.Components.ContainsKey(componentType))
        {
            Component newComponent = HellenicMessageToComponentConverter.GetComponentByType(componentType);
            vehicle.Components.Add(componentType, newComponent);
        }

        HellenicMessageToComponentConverter.UpdateComponent(message,
            vehicle.Components.GetValueOrDefault(componentType));

        int x = 5;
    }

    // Clean up any stale vehicles
    void CleanupVehicles()
    {
        // TODO::ARGYRASPIDES() { Maybe have some timer here for staleness and then just remove from dictionary }
    }

    void OnHellenicMessageReceived(HellenicMessage message)
    {
        TryAddVehicle(message);
        UpdateVehicle(message);
        CleanupVehicles();
    }
}
