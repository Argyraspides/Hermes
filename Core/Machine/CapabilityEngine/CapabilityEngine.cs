using System.Collections.Generic;
using Hermes.Core.Machine.Machine;
namespace Hermes.Core.Machine.CapabilityEngine;


// todo: capability engine
// idea is that this will figure out the capabilities of a particular machine dynamically through behavioral analysis
// and also basic assumptions (e.g., quadcopter must have takeoff capability)
// right now it feels like a static utility class but later should be some sort of component registered into the machine manager
// and will dynaimcally inject capabilities into machine objects
// TOOD: make sure to manage thread safety coz capability engine will be injecting the hashset of machine
// with capabilities
public class CapabilityEngine
{
    public CapabilityEngine()
    {

    }

    // API to dynamically determine the capabilities of a machine and inject it with those found capabilities
    public void DetermineCapabilities(Machine.Machine machine)
    {
        InjectCapabilities(machine);
    }

    private void InjectCapabilities(Core.Machine.Machine.Machine machine)
    {
        switch (machine.MachineType)
        {
            case MachineType.Quadcopter:
                machine.Capabilities = GetQuadcopterCapabilities();
                break;
        }
    }

    private HashSet<Capability> GetQuadcopterCapabilities()
    {
        return new HashSet<Capability>()
        {
            Capability.MULTIROTOR_TAKEOFF,
            Capability.MULTIROTOR_LANDING
        };
    }
}
