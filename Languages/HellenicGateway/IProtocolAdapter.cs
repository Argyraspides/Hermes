/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


namespace Hermes.Languages.HellenicGateway;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

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
