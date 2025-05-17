using System;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

public class MAVLinkCommandFactory : IDisposable
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


    public void TakeoffQuadcopter(Machine machine, double altitude)
    {
        if (!MachineValid(machine)) return;
        if (machine.MachineType != MachineType.Quadcopter)
        {
            HermesUtils.HermesLogWarning("Cannot send takeoff command to a non-quadcopter machine");
            return;
        }

        // TODO::ARGYRASPIDES() { Eh ... if we only need to string together a few dependent commands thats fine but this can easily turn into a nested nightmare.
        //  find a better way to do it .... }
        m_mavLinkCommander.SendMAVLinkArmCommand(machine, false, (armSuccess) =>
        {
            if (armSuccess)
            {
                m_mavLinkCommander.SendMAVLinkTakeoffCommand(machine, altitude);
            }
        });
    }

    public void LandQuadcopter(Machine machine, double abortAltitude = 0.0)
    {
        m_mavLinkCommander.SendMAVLinkLandCommand(machine, abortAltitude);
    }

    public void Dispose()
    {
        m_mavLinkCommander?.Dispose();
    }
}
