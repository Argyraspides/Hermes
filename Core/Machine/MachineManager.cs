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


using Hermes.Universe.Autoloads.EventBus;

namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;

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
    }

    public override void _Process(double delta)
    {
        foreach (Machine machine in m_Machines.Values)
        {
            double timeElapsed = Time.GetUnixTimeFromSystem() - machine.LastUpdateTimeUnix;
            if (timeElapsed > MACHINE_STALE_TIME_S)
            {
                m_Machines.Remove(machine.Identity.MachineId);
                EmitSignal(SignalName.MachineDisconnected, machine);
            }
        }
    }

    void UpdateMachine(HellenicMessage message)
    {
        if (!m_Machines.ContainsKey(message.MachineId))
        {
            m_Machines[message.MachineId] = new Machine();
            EmitSignal(SignalName.NewMachineConnected, m_Machines[message.MachineId]);
        }
        Machine machine = m_Machines[message.MachineId];
        machine.Update(message);
    }

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        UpdateMachine(message);
    }

    public Machine GetMachine(uint entityId)
    {
        if (m_Machines.ContainsKey(entityId))
        {
            return m_Machines[entityId];
        }
        return null;
    }
}
