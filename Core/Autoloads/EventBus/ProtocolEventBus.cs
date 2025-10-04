using Godot;

namespace Hermes.Core.Autoloads.EventBus;

public partial class ProtocolEventBus : Node
{

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    public void OnHellenicMessageReceived(HellenicMessage message)
    {
        EmitSignal(SignalName.HellenicMessageReceived, message);
    }
}
