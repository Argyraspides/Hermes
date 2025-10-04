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


using Hermes.Common.HermesUtils;
namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Core.Machine.CapabilityEngine;
using Hermes.Core.Autoloads.EventBus;

public partial class MachineManager : Node
{
    [Signal]
    public delegate void NewMachineConnectedEventHandler(Core.Machine.Machine.Machine machine);

    [Signal]
    public delegate void MachineDisconnectedEventHandler(Core.Machine.Machine.Machine machine);

    private CapabilityEngine.CapabilityEngine m_capabilityEngine = new CapabilityEngine.CapabilityEngine();

    private Dictionary<uint, Core.Machine.Machine.Machine> m_Machines = new Dictionary<uint, Core.Machine.Machine.Machine>();

    private readonly int MACHINE_STALE_TIME_S = 5;

    public override void _Ready()
    {
        Autoloads.EventBus.GlobalEventBus.Instance.ProtocolEventBus.HellenicMessageReceived += OnHellenicMessageReceived;

        NewMachineConnected += Autoloads.EventBus.GlobalEventBus.Instance.MachineEventBus.OnNewMachineConnected;
        MachineDisconnected += Autoloads.EventBus.GlobalEventBus.Instance.MachineEventBus.OnMachineDisconnected;
    }

    // todo: try make event based? Dont wanna go through the machine list every frame but eh game loop things ig
    public override void _Process(double delta)
    {
        foreach (Core.Machine.Machine.Machine machine in m_Machines.Values)
        {
            double timeElapsed = Time.GetUnixTimeFromSystem() - machine.LastUpdateTimeUnix;
            if (machine.MachineId.HasValue && timeElapsed > MACHINE_STALE_TIME_S)
            {
                machine.QueueFree();
                m_Machines.Remove(machine.MachineId.Value);
                HermesUtils.HermesLogWarning($"Machine with ID {machine.MachineId.Value} has disconnected.");
                EmitSignal(SignalName.MachineDisconnected, machine);
            }
        }
    }

    Machine.Machine TryAddMachine(HellenicMessage message)
    {
        if (!message.Id.HasValue || !message.MachineId.HasValue)
        {
            return null;
        }

        if (!m_Machines.ContainsKey(message.MachineId.Value))
        {
            var machineCardScene = GD.Load<PackedScene>("res://Core/Machine/Machine/Machine.tscn");
            var machineCardInstance = machineCardScene.Instantiate<Core.Machine.Machine.Machine>();
            m_Machines[message.MachineId.Value] = machineCardInstance;
            AddChild(m_Machines[message.MachineId.Value]);
            HermesUtils.HermesLogInfo($"Machine with ID {message.MachineId.Value} has connected.");
            EmitSignal(SignalName.NewMachineConnected, m_Machines[message.MachineId.Value]);
        }

        return m_Machines[message.MachineId.Value];
    }

    void Update(HellenicMessage message)
    {
        Machine.Machine machine = TryAddMachine(message);
        if (machine == null)
        {
            return;
        }

        // In MachineManagerUpdater.cs
        UpdateMachine(machine, message);
        m_capabilityEngine.DetermineCapabilities(machine);
    }

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        Update(message);
    }
}
