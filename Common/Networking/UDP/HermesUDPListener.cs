using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hermes.Common.Networking.UDP;

/*
 * TODO::ARGYRASPIDES() {
 *      Come up with a hashing function to convert IP:Port and IP:Port:SubID into unique uint's
 *      to make retrieval much faster in the ConcurrentDictionaries
 *  }
 */
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

    // IP:Port:SubID -> Callback
    private static ConcurrentDictionary<string, Action<string>> subCallbacks =
        new ConcurrentDictionary<string, Action<string>>();

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

    public static string RegisterUdpClient(IPEndPoint ipEndpoint, Action<string> callback)
    {
        lock (registrationLock)
        {
            string epKey = GetEndpointKey(ipEndpoint);
            string subKey = GetSubKey(nextId, ipEndpoint);

            udpClients.GetOrAdd(epKey, (_) => { return new UdpClient(ipEndpoint); });
            buffers.GetOrAdd(subKey, new ConcurrentQueue<UdpReceiveResult>());
            subCallbacks.GetOrAdd(subKey, callback);
            nextId++;

            return subKey;
        }
    }

    public static void DeregisterUdpClient(string subKey)
    {
        buffers.TryRemove(subKey, out _);
        subCallbacks.TryRemove(subKey, out _);

        string[] parts = subKey.Split(':');
        string endpointKey = $"{parts[0]}:{parts[1]}";
        // Number of subscribers with same endpoint
        int subsLeft = buffers.Keys.Where(key => key.Contains(endpointKey)).Count();

        if (subsLeft == 0)
        {
            udpClients.TryRemove(endpointKey, out _);
        }
    }

    public static UdpReceiveResult Receive(string subKey)
    {
        if (!buffers.TryGetValue(subKey, out ConcurrentQueue<UdpReceiveResult> buff))
        {
            HermesUtils.HermesUtils.HermesLogError($"Unable to find subscriber with ID of {subKey}!");
            return new UdpReceiveResult();
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
                UdpReceiveResult data = await client.Value.ReceiveAsync(ct);

                foreach (KeyValuePair<string, ConcurrentQueue<UdpReceiveResult>> buff in buffers)
                {
                    if (!buff.Key.Contains(client.Key)) continue;

                    if (buff.Value.Count == MAX_BUFFER_SIZE)
                    {
                        buff.Value.TryDequeue(out _);
                    }

                    buff.Value.Enqueue(data);

                    // Let subscriber know udp message has been received
                    subCallbacks.TryGetValue(buff.Key, out Action<string> subCallback);
                    subCallback?.Invoke(buff.Key);
                }
            }
        }
    }

    public static void Dispose()
    {

        HermesUtils.HermesUtils.HermesLogInfo("Destroying HermesUDPListener ...");

        cancellationTokenSource.Cancel();
        listenerThread.Join();

        foreach (UdpClient client in udpClients.Values)
        {
            client.Close();
            client.Dispose();
        }

        HermesUtils.HermesUtils.HermesLogInfo("Finished destroying HermesUDPListener!");

    }
}
