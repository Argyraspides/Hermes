using System.Collections.Generic;
using Hermes.Core.Machine.Machine.Capabilities;

namespace Hermes.Core.Machine.Machine;

public static class MachineUtils
{

    public static IEnumerable<Capability> GetCapabilities(Machine machine)
    {
        switch (machine.MachineType)
        {
            case MachineType.Quadcopter:
                return GetQuadcopterCapabilities(machine);
            default:
                return new List<Capability>() { };
        }
    }

    public static IEnumerable<Capability> GetQuadcopterCapabilities(Machine machine)
    {
        return new List<Capability>()
        {
            Capability.Takeoff,
            Capability.Landing
        };
    }

}
