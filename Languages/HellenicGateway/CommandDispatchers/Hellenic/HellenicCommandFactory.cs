using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;
using Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;

public class HellenicCommandFactory
{
    private MAVLinkCommandFactory m_mavlinkCommandFactory = new MAVLinkCommandFactory();

    private bool MachineValid(Machine machine)
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

        HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.Pulse);

        switch (msg.OriginalProtocol)
        {
            case (uint)Protocols.Mavlink:
                m_mavlinkCommandFactory.TakeoffQuadcopter(machine, altitude);
                break;
        }
    }

    public void LandQuadcopter(Machine machine, double abortAltitude = 0.0d)
    {
        if (!MachineValid(machine)) return;

        HellenicMessage msg = machine.GetHellenicMessage(HellenicMessageType.Pulse);

        switch (msg.OriginalProtocol)
        {
            case (uint)Protocols.Mavlink:
                m_mavlinkCommandFactory.LandQuadcopter(machine, abortAltitude);
                break;
        }
    }

}
