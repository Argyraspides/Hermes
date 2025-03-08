using System.Collections.Generic;
using Godot;
using Hermes.Common.Communications.WorldListener;
using Hermes.Languages.HellenicGateway.Adapters;

namespace Hermes.Languages.HellenicGateway;

public partial class ProtocolManager : Node
{
    public ProtocolManager()
    {
    }

    List<IProtocolAdapter> m_protocolAdapters = new List<IProtocolAdapter>() { new MAVLinkAdapter() };
    ICommandDispatcher m_commandDispatcher;

    public override void _Ready()
    {
        var worldListener = WorldListener.Instance;
        worldListener.WebSocketPacketReceived += OnWorldListenerWebSocketPacketReceived;
    }


    void OnWorldListenerWebSocketPacketReceived(byte[] websocketMessage)
    {
        for (int i = 0; i < m_protocolAdapters.Count; i++)
        {
            if (m_protocolAdapters[i].IsOfProtocolType(websocketMessage))
            {
                m_protocolAdapters[i].HandleMessage(websocketMessage);
                return;
            }
        }
    }
}
