using System.Collections.Generic;
using Godot;
using Hermes.Common.HermesUtils;
using Hermes.Core.Autoloads.EventBus;

namespace Hermes.Core.StateManagers;


public partial class SelectionModel : Node
{

    Dictionary<uint, Machine.Machine.Machine> m_selectedMachines = new Dictionary<uint, Machine.Machine.Machine>();

    public override void _Ready()
    {
        GlobalEventBus.Instance.UIEventBus.MachineSelected += OnMachineClicked;
        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineClicked;
    }

    private void OnMachineClicked(Machine.Machine.Machine machine)
    {

        if (machine == null || !machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogWarning("SelectionModel::OnMachineClicked(): Machine or its ID is null!");
            return;
        }

        if (m_selectedMachines.ContainsKey(machine.MachineId.Value))
        {
            m_selectedMachines.Remove(machine.MachineId.Value);
            HermesUtils.HermesLogInfo($"Deselecting machine: {machine.MachineId.Value}");
        }
        else
        {
            m_selectedMachines.TryAdd(machine.MachineId.Value, machine);
            HermesUtils.HermesLogInfo($"Selecting machine: {machine.MachineId.Value}");
        }

    }


}
