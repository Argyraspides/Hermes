using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Hermes.Common.Communications.WorldListener.MAVLink;

// See: https://mavlink.io/en/guide/serialization.html
public class MAVLinkUDPListener
{
    private Dictionary<IPEndPoint, UdpClient> m_udpClients;

    // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
    private ConcurrentQueue<global::MAVLink.MAVLinkMessage> m_messageQueue;

    private int m_maxMessageBufferSize = 4096;

    Thread m_udpListenerThread;
    private CancellationTokenSource m_cancellationTokenSource;

    public MAVLinkUDPListener(params IPEndPoint[] endPoints)
    {
        m_udpClients = new Dictionary<IPEndPoint, UdpClient>();

        // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
        m_messageQueue = new ConcurrentQueue<global::MAVLink.MAVLinkMessage>();

        m_cancellationTokenSource = new CancellationTokenSource();

        foreach (IPEndPoint endPoint in endPoints)
        {
            m_udpClients.Add(endPoint, new UdpClient(endPoint));
        }
    }

    public void StartListeningThread()
    {
        m_udpListenerThread = new Thread(StartListening) { IsBackground = true };
        m_udpListenerThread.Start(m_cancellationTokenSource.Token);
    }

    // TODO::ARGYRASPIDES() { See if you can offload this to the MAVLink library. OK for now. }
    public bool IsOfProtocolType(byte[] rawPacket)
    {
        if (rawPacket == null || rawPacket.Length < 1)
            return false;
        return rawPacket[0] == global::MAVLink.MAVLINK_STX || rawPacket[0] == global::MAVLink.MAVLINK_STX_MAVLINK1;
    }

    private async void StartListening(object pCancellationToken)
    {
        CancellationToken cancellationToken = (CancellationToken)pCancellationToken;
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var udpClient in m_udpClients.Values)
            {
                UdpReceiveResult dat;
                try
                {
                    dat = await udpClient.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (!IsOfProtocolType(dat.Buffer))
                {
                    continue;
                }

                if (m_messageQueue.Count >= m_maxMessageBufferSize)
                {
                    m_messageQueue.TryDequeue(out _);
                }
                m_messageQueue.Enqueue(new global::MAVLink.MAVLinkMessage(dat.Buffer));

            }
        }
    }

    public void StopListening()
    {
        m_cancellationTokenSource.Cancel();

        foreach (var udpClient in m_udpClients.Values)
        {
            udpClient.Close();
            udpClient.Dispose();
        }

        m_udpListenerThread.Join();
    }

    public global::MAVLink.MAVLinkMessage GetNextMessage()
    {
        global::MAVLink.MAVLinkMessage msg = null;
        m_messageQueue.TryDequeue(out msg);
        return msg;
    }
}
