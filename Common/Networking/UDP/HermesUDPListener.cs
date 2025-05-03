using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hermes.Common.Networking.UDP;

public class HermesUDPListener
{
    private const uint MAX_BUFFER_SIZE = 512;

    private static uint nextId = 0;

    // IP:Port -> UdpClient
    private static ConcurrentDictionary<string, UdpClient> udpClients
        = new ConcurrentDictionary<string, UdpClient>();

    // IP:Port:SubID -> Buffer
    private static ConcurrentDictionary<string, ConcurrentQueue<UdpReceiveResult>> buffers
        = new ConcurrentDictionary<string, ConcurrentQueue<UdpReceiveResult>>();

    private static Thread listenerThread;
    private static CancellationTokenSource cancellationTokenSource;

    private static object registrationLock = new object();

    static HermesUDPListener()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, __) => { Dispose(); };
        cancellationTokenSource = new CancellationTokenSource();
        listenerThread = new Thread(() => { ListenUDP(cancellationTokenSource.Token); });
        listenerThread.Start();
    }

    public static uint RegisterUdpClient(IPEndPoint ipEndpoint)
    {
        lock (registrationLock)
        {
            string epKey = GetEndpointKey(ipEndpoint);
            string subKey = GetSubKey(nextId, ipEndpoint);

            udpClients.GetOrAdd(epKey, client => { return new UdpClient(ipEndpoint); });
            buffers.GetOrAdd(subKey, new ConcurrentQueue<UdpReceiveResult>());

            return nextId++;
        }
    }

    public static void DeregisterUdpClient(uint id, IPEndPoint endpoint)
    {
        string subKey = GetSubKey(id, endpoint);
        buffers.TryRemove(subKey, out _);

        // Number of subscribers with same endpoint
        string endpointKey = GetEndpointKey(endpoint);
        int subsLeft = buffers.Keys.Where(key => key.Contains(endpointKey)).Count();

        if (subsLeft == 0)
        {
            udpClients.TryRemove(endpointKey, out _);
        }
    }

    public static UdpReceiveResult Receive(uint id, IPEndPoint ipEndpoint)
    {
        string subKey = GetSubKey(id, ipEndpoint);

        if (!buffers.TryGetValue(subKey, out ConcurrentQueue<UdpReceiveResult> buff))
        {
            throw new KeyNotFoundException($"Unable to find subscriber with ID of {subKey}");
        }

        buff.TryDequeue(out UdpReceiveResult res);
        return res;
    }

    private static string GetEndpointKey(IPEndPoint ipEndpoint)
    {
        // Normalize special addresses like 0.0.0.0 or ::0
        string address = ipEndpoint.Address.ToString();
        if (ipEndpoint.Address.Equals(IPAddress.Any) ||
            ipEndpoint.Address.Equals(IPAddress.IPv6Any))
        {
            address = "Any";
        }

        return $"{address}:{ipEndpoint.Port}";
    }

    private static string GetSubKey(uint id, IPEndPoint ipEndpoint)
    {
        return $"{GetEndpointKey(ipEndpoint)}:{id}";
    }

    private static async void ListenUDP(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (KeyValuePair<string, UdpClient> client in udpClients)
            {
                UdpReceiveResult data = await client.Value.ReceiveAsync();

                foreach (KeyValuePair<string, ConcurrentQueue<UdpReceiveResult>> buff in buffers)
                {
                    if (!buff.Key.Contains(client.Key)) continue;

                    if (buff.Value.Count == MAX_BUFFER_SIZE)
                    {
                        buff.Value.TryDequeue(out _);
                    }

                    buff.Value.Enqueue(data);
                }
            }
        }
    }

    public static void Dispose()
    {
        cancellationTokenSource.Cancel();
        listenerThread.Join();

        foreach (UdpClient client in udpClients.Values)
        {
            client.Close();
            client.Dispose();
        }
    }
}
