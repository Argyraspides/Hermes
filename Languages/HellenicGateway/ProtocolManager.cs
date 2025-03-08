using Godot;

namespace Hermes.Language.HellenicGateway;

// TODO(Argyraspides, 02/03/2025) TEMPORARY: Find a better place to put this. Looks pretty clean this way, though
// also remove this as a singleton
public partial class ProtocolManager : Node
{
    public override void _Ready()
    {
    }

    void OnMAVLinkJSONMessageReceived(byte[] rawData)
    {
    }
}
