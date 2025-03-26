using Godot;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class ProtocolEventBus : Node
{
    private ProtocolManager m_protocolManager;

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    private void OnHellenicMessageReceived(HellenicMessage message)
    {
        EmitSignal(SignalName.HellenicMessageReceived, message);
    }

    public void LoadProtocolManagerNode()
    {
        m_protocolManager = new ProtocolManager();
        AddChild(m_protocolManager);
    }

    public void ConnectProtocolManagerNode()
    {
        m_protocolManager.HellenicMessageReceived += OnHellenicMessageReceived;
    }
}
