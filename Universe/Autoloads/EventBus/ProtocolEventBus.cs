using Godot;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Universe.Autoloads.EventBus;

public partial class ProtocolEventBus : Node
{

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    public void OnHellenicMessageReceived(HellenicMessage message)
    {
        EmitSignal(SignalName.HellenicMessageReceived, message);
    }
}
