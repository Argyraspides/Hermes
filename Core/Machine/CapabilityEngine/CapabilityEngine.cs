using System.Collections.Generic;
using Hermes.Core.Machine.Machine;
namespace Hermes.Core.Machine.CapabilityEngine;

public class CapabilityEngine
{
    public CapabilityEngine()
    {

    }

    public static IEnumerable<Capability> GetCapabilities(Core.Machine.Machine.Machine machine)
    {
        switch (machine.MachineType)
        {
            case MachineType.Quadcopter:
                return GetQuadcopterCapabilities(machine);
            default:
                return new List<Capability>() { };
        }
    }

    public static IEnumerable<Capability> GetQuadcopterCapabilities(Core.Machine.Machine.Machine machine)
    {
        return new List<Capability>()
        {
            Capability.MULTIROTOR_TAKEOFF,
            Capability.MULTIROTOR_LANDING
        };
    }
}
