using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Hermes.Languages.HellenicGateway;

/// <summary>
/// A protocol adapters job is to listen for messages of a specific protocol, convert them into Hellenic,
/// and buffer them internally such that they can be accessed at any time. In addition to this, a protocol adapter
/// must handle any protocol behaviors (e.g., via a state machine) such as acknowledgements, timeouts, etc.
/// </summary>
public interface IProtocolAdapter
{
    // Starts the protocol adapter by initializing & starting all of its listeners
    void Start();

    // Stops the protocol adapters by properly freeing all listening resources (e.g., UDP clients) and
    // cleaning up any threads
    void Stop();

    // Gets the next Hellenic message in the adapters buffer
    HellenicMessage GetNextHellenicMessage();
}
