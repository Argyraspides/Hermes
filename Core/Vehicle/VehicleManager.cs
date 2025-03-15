using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle.Components;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Core.Vehicle;

public partial class VehicleManager : Node
{
    private Dictionary<uint, Vehicle> m_Vehicles = new Dictionary<uint, Vehicle>();

    public override void _Ready()
    {
        ProtocolManager.Instance.HellenicMessageReceived += OnHellenicMessageReceived;
    }

    void OnHellenicMessageReceived(HellenicMessage message)
    {
        if (!m_Vehicles.ContainsKey(message.ID))
        {
            m_Vehicles.Add(message.ID, new Vehicle());
        }

        if (message.MessageName == nameof(LatitudeLongitude))
        {
            m_Vehicles[message.ID].Components.Add(ComponentType.GPS, new GPSComponent());
        }

        int x = 5;
    }
}
