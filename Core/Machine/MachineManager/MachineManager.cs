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


using Hermes.Common.Map.Utils;

namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Universe.Autoloads.EventBus;

public partial class MachineManager : Node
{
    [Signal]
    public delegate void NewMachineConnectedEventHandler(Machine machine);

    [Signal]
    public delegate void MachineDisconnectedEventHandler(Machine machine);

    private Dictionary<uint, Machine> m_Machines = new Dictionary<uint, Machine>();

    private readonly int MACHINE_STALE_TIME_S = 5;

    public override void _Ready()
    {

        GlobalEventBus.Instance.ProtocolEventBus.HellenicMessageReceived += OnHellenicMessageReceived;

        NewMachineConnected += GlobalEventBus.Instance.MachineEventBus.OnNewMachineConnected;
        MachineDisconnected += GlobalEventBus.Instance.MachineEventBus.OnMachineDisconnected;
    }

    public override void _Process(double delta)
    {
        foreach (Machine machine in m_Machines.Values)
        {
            double timeElapsed = Time.GetUnixTimeFromSystem() - machine.LastUpdateTimeUnix;
            if (machine.MachineId.HasValue && timeElapsed > MACHINE_STALE_TIME_S)
            {
                m_Machines.Remove(machine.MachineId.Value);
                EmitSignal(SignalName.MachineDisconnected, machine);
            }
        }
    }

    void UpdateMachine(HellenicMessage message)
    {
        if (!message.Id.HasValue || !message.MachineId.HasValue) return;

        if (!m_Machines.ContainsKey(message.MachineId.Value))
        {
            var machineCardScene = GD.Load<PackedScene>("res://Core/Machine/Machine/Machine.tscn");
            var machineCardInstance = machineCardScene.Instantiate<Machine>();
            m_Machines[message.MachineId.Value] = machineCardInstance;
            AddChild(m_Machines[message.MachineId.Value]);
            EmitSignal(SignalName.NewMachineConnected, m_Machines[message.MachineId.Value]);
        }
        Machine machine = m_Machines[message.MachineId.Value];
        machine.Update(message);
    }

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateMachine(message);
    }
}
