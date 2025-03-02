using System.Collections.Generic;
using Godot;
using Hermes.Conversion;
using Hermes.Dialect.Hellenic;
using Newtonsoft.Json.Linq;

namespace Hermes.Language.HellenicGateway;

// TODO(Argyraspides, 02/03/2025) TEMPORARY: Find a better place to put this. Looks pretty clean this way, though
// also remove this as a singleton
public partial class HellenicGateway : Node
{
    [Signal]
    public delegate void HellenicMessagesReceivedEventHandler(List<IHellenicMessage> messages);

    MAVLinkConverterFactory m_mavLinkConverterFactory;

    public override void _Ready()
    {
        m_mavLinkConverterFactory = new MAVLinkConverterFactory();

        var WorldListener = Common.Communications.WorldListener.WorldListener.Instance;
        // TODO(Argyraspides, 2/03/2025) You should check first that we indeed have a MAVLink message before doing this
        WorldListener.WebSocketPacketReceived += OnMAVLinkJSONMessageReceived;
    }

    void OnMAVLinkJSONMessageReceived(byte[] rawData)
    {
        if (rawData == null || rawData.Length == 0)
        {
            return;
        }

        string jsonString = System.Text.Encoding.UTF8.GetString(rawData);
        JObject j = JObject.Parse(jsonString);
        List<IHellenicMessage> messages = m_mavLinkConverterFactory.Convert(j);
    }
}
