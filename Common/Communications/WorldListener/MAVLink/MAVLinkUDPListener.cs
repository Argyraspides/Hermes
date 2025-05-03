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
    private Dictionary<string, IPEndPoint> m_udpEndpoints;

    // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
    private ConcurrentQueue<global::MAVLink.MAVLinkMessage> m_messageQueue;
    public event Action MAVLinkMessageReceived;

    public event Action<string> HermesUDPDatagramReceived;

    private int m_maxMessageBufferSize = 45;

    Thread m_udpListenerThread;
    private CancellationTokenSource m_cancellationTokenSource;

    public MAVLinkUDPListener(params IPEndPoint[] endPoints)
    {

        m_udpEndpoints = new Dictionary<string, IPEndPoint>();

        // MAVLink.MAVLinkMessage is auto generated code. Ensure you've auto-generated the MAVLink headers
        m_messageQueue = new ConcurrentQueue<global::MAVLink.MAVLinkMessage>();

        m_cancellationTokenSource = new CancellationTokenSource();

        foreach (IPEndPoint endPoint in endPoints)
        {
            string id = HermesUDPListener.RegisterUdpClient(endPoint, HermesUDPDatagramReceived);
            m_udpEndpoints.Add(id, endPoint);
        }
    }

    public void StartListeningThread()
    {
        m_udpListenerThread = new Thread(StartListening) { IsBackground = true };
        m_udpListenerThread.Start(m_cancellationTokenSource.Token);
    }

    private void StartListening(object pCancellationToken)
    {
        CancellationToken cancellationToken = (CancellationToken)pCancellationToken;
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var endpoint in m_udpEndpoints)
            {

                var dat = HermesUDPListener.Receive(endpoint.Key);

                if (dat.Buffer.IsEmpty())
                {
                    continue;
                }

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
                    // IMPORTANT:
                    //  There is a bug where immediately after sending a command, a set of four different telemetry messages
                    //  (with MAVLink message IDs of 31, 83, 141, and 30) arrive at the UDP socket truncated and full one after another.
                    //  I literally have no idea why. It doesn't pose any issue as MAVLink telemetry messages are sent in like a machine gun
                    HermesUtils.HermesUtils.HermesLogBullshit("Received truncated MAVLink message!");
                }
            }
        }
    }

    public void StopListening()
    {
        m_cancellationTokenSource.Cancel();

        foreach (var subs in m_udpEndpoints)
        {
            HermesUDPListener.DeregisterUdpClient(subs.Key);
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
