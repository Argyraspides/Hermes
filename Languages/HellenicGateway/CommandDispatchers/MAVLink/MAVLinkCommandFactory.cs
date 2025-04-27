using System;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

public class MAVLinkCommandFactory
{
    private MAVLinkCommander m_mavLinkCommander = new MAVLinkCommander();

    public bool MachineValid(Machine machine)
    {
        if (machine == null)
        {
            HermesUtils.HermesLogError("Cannot send command to a null machine");
            return false;
        }

        if (!machine.MachineId.HasValue)
        {
            HermesUtils.HermesLogError("Cannot send takeoff command to a machine with an unknown ID");
            return false;
        }

        return true;
    }


    public async void TakeoffQuadcopter(Machine machine, double altitude)
    {
        if (!MachineValid(machine)) return;
        if (machine.MachineType != MachineType.Quadcopter)
        {
            HermesUtils.HermesLogWarning("Cannot send takeoff command to a non-quadcopter machine");
            return;
        }

        await m_mavLinkCommander.SendMAVLinkArmCommand(machine);
        await m_mavLinkCommander.SendMAVLinkTakeoffCommand(machine, altitude);
    }

    public async void LandQuadcopter(Machine machine, double abortAltitude = 0.0)
    {
        await m_mavLinkCommander.SendMAVLinkLandCommand(machine, abortAltitude);
    }


}
