using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using Godot;
using Hermes.Common.Communications.WorldListener;
using Hermes.Common.Communications.WorldListener.MAVLink;

namespace Hermes.Languages.HellenicGateway.Adapters;

using System.Collections.Generic;
using Hermes.Languages.HellenicGateway.StateMachines;

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
    private MAVLinkStateMachine m_mavlinkStateMachine = new MAVLinkStateMachine();

    private MAVLinkUDPListener m_udpListener = new MAVLinkUDPListener(
        // new IPEndPoint(IPAddress.Parse("127.0.0.1"), KnownWorlds.DEFAULT_MAVLINK_PORT),
        new IPEndPoint(IPAddress.Parse("127.0.0.1"), 14445)
    );


    private Thread m_hellenicProcessorThread;
    private CancellationTokenSource m_cancellationTokenSource;

    private ConcurrentQueue<HellenicMessage> m_messageQueue;
    private int m_maxMessageQueueSize = 4096;

    ~MAVLinkAdapter()
    {
        m_udpListener.StopListening();
    }

    public List<HellenicMessage> HandleMessage(MAVLink.MAVLinkMessage fullMAVLinkMessage)
    {
        List<HellenicMessage> hellenicMessages = new List<HellenicMessage>();
        if (fullMAVLinkMessage == null)
        {
            return hellenicMessages;
        }

        hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(fullMAVLinkMessage);

        switch (fullMAVLinkMessage.msgid)
        {
            case (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                MAVLink.mavlink_heartbeat_t heartbeatMessage =
                    MavlinkUtil.ByteArrayToStructure<MAVLink.mavlink_heartbeat_t>(fullMAVLinkMessage.buffer);
                m_mavlinkStateMachine.HandleHeartBeatMessage(fullMAVLinkMessage, heartbeatMessage);
                break;
            case (uint)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                MAVLink.mavlink_global_position_int_t globalPositionIntMsg =
                    MavlinkUtil.ByteArrayToStructure<MAVLink.mavlink_global_position_int_t>(fullMAVLinkMessage.buffer);
                m_mavlinkStateMachine.HandleGlobalPositionIntMessage(fullMAVLinkMessage, globalPositionIntMsg);
                break;
        }

        return hellenicMessages;
    }

    public void Start()
    {
        m_messageQueue = new ConcurrentQueue<HellenicMessage>();
        m_cancellationTokenSource = new CancellationTokenSource();

        m_udpListener.StartListeningThread();
        m_hellenicProcessorThread = new Thread(StartMessageProcessor);
        m_hellenicProcessorThread.Start();

    }

    private void StartMessageProcessor()
    {
        // TODO::ARGYRASPIDES(10/03/2025) { Don't do a busy wait like this. Make it event based. Probably a callback for when a UDP message is ready }
        while (true)
        {
            if (m_udpListener.GetNextMessage() is MAVLink.MAVLinkMessage msg)
            {
                List<HellenicMessage> hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(msg);
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
    }

    public void Stop()
    {
        m_udpListener.StopListening();
        m_hellenicProcessorThread.Join();
    }

    public HellenicMessage GetNextHellenicMessage()
    {
        HellenicMessage msg = null;
        m_messageQueue.TryDequeue(out msg);
        GD.Print("Hellenic buffer size: " + m_messageQueue.Count);
        return msg;
    }
}
