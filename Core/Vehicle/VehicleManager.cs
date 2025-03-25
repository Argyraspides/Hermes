/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


namespace Hermes.Core.Vehicle;

using Godot;
using System.Collections.Generic;
using Hermes.Core.Vehicle.Components;
using Hermes.Universe.Autoloads;

public partial class VehicleManager : Node
{

    [Signal]
    public delegate void NewVehicleConnectedEventHandler(Vehicle vehicle);

    private Dictionary<uint, Vehicle> m_Vehicles = new Dictionary<uint, Vehicle>();

    public override void _Ready()
    {
        EventBus.Instance.HellenicMessageReceived += OnHellenicMessageReceived;
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
        vehicle.Update(message);
    }

    // Clean up any stale vehicles
    void CleanupVehicles()
    {
        // TODO::ARGYRASPIDES() { Maybe have some timer here for staleness and then just remove from dictionary }
    }

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateVehicle(message);
        CleanupVehicles();
    }
}
