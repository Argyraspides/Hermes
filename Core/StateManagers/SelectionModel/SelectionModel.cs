using System.Collections.Generic;
using System.Linq;
using Godot;
using Daedalus.Logging;
using Hermes.Core.Autoloads.EventBus;

namespace Hermes.Core.StateManagers.SelectionModel;


public partial class SelectionModel : Node
{

    [Signal]
    public delegate void FocussedMachineChangedEventHandler(Machine.Machine.Machine machine);

    Dictionary<uint, Machine.Machine.Machine> m_selectedMachines = new Dictionary<uint, Machine.Machine.Machine>();

    public override void _Ready()
    {
        Logger.LogInfo(this, "SelectionModel::_Ready()");
        HermesEventBus.Instance.UIEventBus.MachineSelected += OnMachineClicked;
        HermesEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineClicked;

        FocussedMachineChanged += HermesEventBus.Instance.UIEventBus.OnFocussedMachineChanged;

    }

    private void OnMachineClicked(Machine.Machine.Machine machine)
    {

        if (machine == null || !machine.MachineId.HasValue)
        {
            Logger.LogWarning(this, "SelectionModel::OnMachineClicked(): Machine or its ID is null!");
            return;
        }

        if (m_selectedMachines.ContainsKey(machine.MachineId.Value))
        {
            m_selectedMachines.Remove(machine.MachineId.Value);
            Logger.LogInfo(this, $"Deselecting machine: {machine.MachineId.Value}");
        }
        else
        {
            m_selectedMachines.TryAdd(machine.MachineId.Value, machine);
            Logger.LogInfo(this, $"Selecting machine: {machine.MachineId.Value}");
        }

        // We got to one by deselecting other machines, or by simply selecting one when we didnt select any before.
        // Either way, we now have a focussed machine
        if (m_selectedMachines.Count == 1)
        {
            Machine.Machine.Machine focussedMachine = m_selectedMachines.First().Value;
            EmitSignal(SignalName.FocussedMachineChanged, focussedMachine);
        }
        else if (m_selectedMachines.Count == 0 || m_selectedMachines.Count > 1)
        {
            // Godot doesn't allow signal emissions with null arguments, so pass in machine that has no ID
            Machine.Machine.Machine nullMachine = new Machine.Machine.Machine();
            EmitSignal(SignalName.FocussedMachineChanged, nullMachine);
        }

    }


}
