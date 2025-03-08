using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hermes.Languages.HellenicGateway;

public struct ConnectionParameters
{
};

public struct CommandResult
{
};

public interface IProtocolAdapter
{
    /// <summary>
    /// Determines whether or not the packet is of this type of protocol.
    /// E.g., "Is this raw packet encoded using the MAVLink protocol?"
    /// </summary>
    /// <param name="rawPacket">The raw packet data</param>
    /// <returns>True if the packet is of this protocol type, false otherwise</returns>
    bool IsOfProtocolType(byte[] rawPacket);

    void HandleMessage(byte[] rawPacket);
}
