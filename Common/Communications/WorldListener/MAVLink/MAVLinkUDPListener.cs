using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Godot;

namespace Hermes.Common.Communications.WorldListener.MAVLink;

// See: https://mavlink.io/en/guide/serialization.html
public class MAVLinkUDPListener
{
    private Dictionary<IPEndPoint, UdpClient> udpClients;

    // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
    private LinkedList<global::MAVLink.MAVLinkMessage> messageQueue;
    private int m_maxMessageBufferSize = 4096;
    private object m_messageQueueLock = new object();

    Thread m_udpListenerThread;
    private volatile bool m_isListening;

    public MAVLinkUDPListener(params IPEndPoint[] endPoints)
    {
        udpClients = new Dictionary<IPEndPoint, UdpClient>();

        // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
        messageQueue = new LinkedList<global::MAVLink.MAVLinkMessage>();

        foreach (IPEndPoint endPoint in endPoints)
        {
            udpClients.Add(endPoint, new UdpClient(endPoint));
        }
    }

    public void StartListeningThread()
    {
        m_udpListenerThread = new Thread(StartListening) { IsBackground = true };
        m_isListening = true;
        m_udpListenerThread.Start();
    }

    private async void StartListening()
    {
        while (m_isListening)
        {
            foreach (var udpClient in udpClients.Values)
            {
                var dat = await udpClient.ReceiveAsync();

                // lock (m_messageQueueLock)
                // {
                    if (messageQueue.Count >= m_maxMessageBufferSize)
                    {
                        continue;//messageQueue.RemoveFirst();
                    }

                    messageQueue.AddLast(
                        new LinkedListNode<global::MAVLink.MAVLinkMessage>(
                            new global::MAVLink.MAVLinkMessage(dat.Buffer)));
                // }
            }
            Thread.Sleep(1000);
        }
    }

    public void StopListening()
    {
        m_isListening = false;
        foreach (var udpClient in udpClients.Values)
        {
            udpClient.Close();
            udpClient.Dispose();
        }

        m_udpListenerThread.Join();
    }

    public global::MAVLink.MAVLinkMessage GetNextMessage()
    {
        global::MAVLink.MAVLinkMessage msg = null;
        // lock (m_messageQueueLock)
        // {
            if (messageQueue.Count > 0)
            {
                msg = messageQueue.First.Value;
                messageQueue.RemoveFirst();
            }
        // }

        return msg;
    }
}
