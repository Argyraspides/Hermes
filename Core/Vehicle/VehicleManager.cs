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
using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Core.Vehicle.Components;
using Hermes.Universe.Autoloads;

public partial class VehicleManager : Node
{
    [Signal]
    public delegate void NewVehicleConnectedEventHandler(Vehicle vehicle);

    [Signal]
    public delegate void VehicleDisconnectedEventHandler(Vehicle vehicle);

    private Dictionary<uint, Vehicle> m_Vehicles = new Dictionary<uint, Vehicle>();

    private readonly int VEHICLE_STALE_TIME_MS = 5;

    public override void _Ready()
    {
        EventBus.Instance.HellenicMessageReceived += OnHellenicMessageReceived;
    }

    public override void _Process(double delta)
    {
        foreach (Vehicle vehicle in m_Vehicles.Values)
        {
            double timeElapsed = Time.GetUnixTimeFromSystem() - vehicle.LastUpdateTimeUnix;
            if (timeElapsed > VEHICLE_STALE_TIME_MS)
            {
                m_Vehicles.Remove(vehicle.Identity.VehicleId);
                EmitSignal(SignalName.VehicleDisconnected, vehicle);
            }
        }
    }
    // TODO::ARGYRASPIDES() { Fix your terminology! You have VehicleId, EntityId, Vehicles and Machines...should be unified
    // make it "MachineId" across the board!!!!!!!!!!!! }
    void UpdateVehicle(HellenicMessage message)
    {
        if (!m_Vehicles.ContainsKey(message.EntityId))
        {
            m_Vehicles[message.EntityId] = new Vehicle();
            EmitSignal(SignalName.NewVehicleConnected, m_Vehicles[message.EntityId]);
        }
        Vehicle vehicle = m_Vehicles[message.EntityId];
        vehicle.Update(message);
    }

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateVehicle(message);
    }

    public Vehicle GetVehicle(uint entityId)
    {
        if (m_Vehicles.ContainsKey(entityId))
        {
            return m_Vehicles[entityId];
        }
        return null;
    }
}
