using System;
using System.Threading.Tasks;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Machine;
using Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;

namespace Hermes.Languages.HellenicGateway.CommandDispatchers.Hellenic;

public class HellenicCommander : IDisposable
{
    private MAVLinkCommandFactory m_mavLinkCommandFactory = new MAVLinkCommandFactory();

    public async Task TakeoffQuadcopter(Machine machine, double altitude)
    {
        if (machine == null)
        {
            HermesUtils.HermesLogError("Cannot send takeoff command to a null machine!");
            return;
        }

        Pulse pulse = (Pulse) machine.GetHellenicMessage(HellenicMessageType.Pulse);

        if (!pulse.OriginalProtocol.HasValue)
        {
            HermesUtils.HermesLogError("Cannot send takeoff command to a machine with unknown protocol");
            return;
        }

        switch (pulse.OriginalProtocol.Value)
        {
            case (uint) Protocols.Mavlink:
                 m_mavLinkCommandFactory.TakeoffQuadcopter(machine, altitude);
                 break;
        }
    }

    public void Dispose()
    {
        m_mavLinkCommandFactory?.Dispose();
    }
}
