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


using Hermes.Universe.Autoloads.EventBus;

namespace Hermes.Languages.HellenicGateway;

using System.Collections.Generic;
using Godot;
using Hermes.Languages.HellenicGateway.Adapters;


public partial class ProtocolManager : Node
{

    public ProtocolManager()
    {
        m_protocolAdapters = new List<IProtocolAdapter>() { new MAVLinkAdapter() };
    }

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    private List<IProtocolAdapter> m_protocolAdapters;

    public override void _Process(double delta)
    {
        ProcessHellenicMessages();
    }

    public override void _Ready()
    {
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            protocolAdapter.Start();
        }

        HellenicMessageReceived += GlobalEventBus.Instance.ProtocolEventBus.OnHellenicMessageReceived;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            protocolAdapter.Stop();
        }
    }

    private void ProcessHellenicMessages()
    {
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            int bufSize = protocolAdapter.GetHellenicBufferSize();
            for (int i = 0; i < bufSize; i++)
            {
                if (protocolAdapter.GetNextHellenicMessage() is HellenicMessage nextMessage)
                {
                    EmitSignal(SignalName.HellenicMessageReceived, nextMessage);
                }
            }
        }
    }
}
