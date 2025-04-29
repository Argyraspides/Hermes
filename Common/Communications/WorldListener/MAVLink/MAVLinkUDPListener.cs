using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Hermes.Common.Networking.UDP;

namespace Hermes.Common.Communications.WorldListener.MAVLink;

// See: https://mavlink.io/en/guide/serialization.html
public class MAVLinkUDPListener
{
    private Dictionary<uint, IPEndPoint> m_udpEndpoints;

    // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
    private ConcurrentQueue<global::MAVLink.MAVLinkMessage> m_messageQueue;
    public event Action MAVLinkMessageReceived;

    private int m_maxMessageBufferSize = 45;

    Thread m_udpListenerThread;
    private CancellationTokenSource m_cancellationTokenSource;

    public MAVLinkUDPListener(params IPEndPoint[] endPoints)
    {

        m_udpEndpoints = new Dictionary<uint, IPEndPoint>();

        // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
        m_messageQueue = new ConcurrentQueue<global::MAVLink.MAVLinkMessage>();

        m_cancellationTokenSource = new CancellationTokenSource();

        foreach (IPEndPoint endPoint in endPoints)
        {
            uint id = HermesUdpClient.RegisterUdpClient(endPoint);
            m_udpEndpoints.Add(id, endPoint);
        }
    }

    public void StartListeningThread()
    {
        m_udpListenerThread = new Thread(StartListening) { IsBackground = true };
        m_udpListenerThread.Start(m_cancellationTokenSource.Token);
    }

    public bool IsMAVLinkPacket(byte[] rawPacket)
    {
        if (rawPacket == null || rawPacket.Length < 1)
        {
            return false;
        }
        return rawPacket[0] == global::MAVLink.MAVLINK_STX || rawPacket[0] == global::MAVLink.MAVLINK_STX_MAVLINK1;
    }

    private async void StartListening(object pCancellationToken)
    {
        CancellationToken cancellationToken = (CancellationToken)pCancellationToken;
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var endpoint in m_udpEndpoints)
            {

                uint id = endpoint.Key;
                IPEndPoint ipEndPoint = endpoint.Value;
                var dat = await HermesUdpClient.ReceiveAsync(id, ipEndPoint);

                if (m_messageQueue.Count >= m_maxMessageBufferSize)
                {
                    m_messageQueue.TryDequeue(out _);
                }

                try
                {
                    m_messageQueue.Enqueue(new global::MAVLink.MAVLinkMessage(dat.Buffer));
                    MAVLinkMessageReceived?.Invoke();
                }
                catch (IndexOutOfRangeException ex)
                {
                    HermesUtils.HermesUtils.HermesLogBullshit("Received truncated MAVLink message!");
                }
            }
        }
    }

    public void StopListening()
    {
        m_cancellationTokenSource.Cancel();

        foreach (var endpoints in m_udpEndpoints)
        {
            HermesUdpClient.DeregisterUdpClient(endpoints.Key, endpoints.Value);
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
