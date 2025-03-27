using Godot;
using Hermes.Core.Machine;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class MachineEventBus : Node
{

    private MachineManager m_MachineManager;

    [Signal]
    public delegate void NewMachineConnectedEventHandler(Machine machine);

    [Signal]
    public delegate void MachineDisconnectedEventHandler(Machine machine);

    private void OnNewMachineConnected(Machine machine)
    {
        EmitSignal(SignalName.NewMachineConnected, machine);
    }

    private void OnMachineDisconnected(Machine machine)
    {
        EmitSignal(SignalName.MachineDisconnected, machine);
    }

    public void LoadMachineManagerNode()
    {
        m_MachineManager = new MachineManager();
        AddChild(m_MachineManager);
    }

    public void ConnectMachineManagerNode()
    {
        m_MachineManager.NewMachineConnected += OnNewMachineConnected;
        m_MachineManager.MachineDisconnected += OnMachineDisconnected;
    }
}
