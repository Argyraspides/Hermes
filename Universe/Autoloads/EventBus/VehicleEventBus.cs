using Godot;
using Hermes.Core.Vehicle;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class VehicleEventBus : Node
{

    private VehicleManager m_vehicleManager;

    [Signal]
    public delegate void NewVehicleConnectedEventHandler(Vehicle vehicle);

    [Signal]
    public delegate void VehicleDisconnectedEventHandler(Vehicle vehicle);

    private void OnNewVehicleConnected(Vehicle vehicle)
    {
        EmitSignal(SignalName.NewVehicleConnected, vehicle);
    }

    private void OnVehicleDisconnected(Vehicle vehicle)
    {
        EmitSignal(SignalName.VehicleDisconnected, vehicle);
    }

    public void LoadVehicleManagerNode()
    {
        m_vehicleManager = new VehicleManager();
        AddChild(m_vehicleManager);
    }

    public void ConnectVehicleManagerNode()
    {
        m_vehicleManager.NewVehicleConnected += OnNewVehicleConnected;
        m_vehicleManager.VehicleDisconnected += OnVehicleDisconnected;
    }
}
