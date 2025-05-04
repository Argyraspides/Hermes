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


using Hermes.Common.Communications.WorldListener;

namespace Hermes.Languages.HellenicGateway.Adapters;

using System.Collections.Concurrent;
using System.Net;
using Hermes.Common.Communications.WorldListener.MAVLink;
using System.Collections.Generic;

/// <summary>
/// The MAVLinkAdapter has the single purpose of listening to MAVLink messages over
/// certain "data links" (such as UDP, TCP, or serial -- currently only UDP is supported)
/// and convert them to Hellenic messages. These Hellenic messages are stored in a buffer
/// which can be retrieved by anything that wishes to use the MAVLinkAdapter. The adapter
/// also has a MAVLinkStateMachine that handles behavioral aspects of MAVLink, such as
/// periodically sending heartbeats.
///
/// The buffer is implemented as a circular queue. When the max queue size is reached,
/// the oldest messages received are removed before adding new ones.
///
/// </summary>
public class MAVLinkAdapter : IProtocolAdapter
{
    private MAVLinkUDPListener m_udpListener = new MAVLinkUDPListener(
        // new IPEndPoint(IPAddress.Parse("127.0.0.1"), KnownWorlds.DEFAULT_MAVLINK_PORT)
        new IPEndPoint(IPAddress.Parse("127.0.0.1"), 14445)
    );

    private ConcurrentQueue<HellenicMessage> m_messageQueue;
    private int m_maxMessageQueueSize = 45;

    ~MAVLinkAdapter()
    {
        m_udpListener.StopListening();
    }

    public void Start()
    {
        m_messageQueue = new ConcurrentQueue<HellenicMessage>();
        m_udpListener.MAVLinkMessageReceived += OnMAVLinkMessageReceived;
    }

    private void OnMAVLinkMessageReceived()
    {
        if (m_udpListener.GetNextMessage() is MAVLink.MAVLinkMessage msg)
        {
            List<HellenicMessage> hellenicMessages =
                MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(msg);
            foreach (HellenicMessage hellenicMessage in hellenicMessages)
            {
                if (m_messageQueue.Count >= m_maxMessageQueueSize)
                {
                    m_messageQueue.TryDequeue(out _);
                }
                m_messageQueue.Enqueue(hellenicMessage);
            }
        }
    }

    public void Stop()
    {
        m_udpListener.StopListening();
    }

    public HellenicMessage GetNextHellenicMessage()
    {
        HellenicMessage msg = null;
        m_messageQueue.TryDequeue(out msg);
        return msg;
    }

    public int GetHellenicBufferSize()
    {
        return m_messageQueue.Count;
    }
}
