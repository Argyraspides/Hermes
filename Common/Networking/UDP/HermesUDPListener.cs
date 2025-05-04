using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hermes.Common.Networking.UDP;

    public static class HermesUDPListener
    {

        private const uint MAX_BUFFER_SIZE = 512;

        private static ushort nextId = 0;

        private static HashSet<ushort> idsInUse
            = new HashSet<ushort>();

        // IP:Port encoded in ulong -> UdpClient
        private static ConcurrentDictionary<ulong, UdpClient> udpClients
            = new ConcurrentDictionary<ulong, UdpClient>();

        // IP:Port:SubID encoded in ulong -> Buffer
        private static ConcurrentDictionary<ulong, ConcurrentQueue<UdpReceiveResult>> buffers
            = new ConcurrentDictionary<ulong, ConcurrentQueue<UdpReceiveResult>>();

        // IP:Port:SubID encoded in ulong -> Callback
        private static ConcurrentDictionary<ulong, Action<ulong>> subCallbacks
            = new ConcurrentDictionary<ulong, Action<ulong>>();

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

        public static ulong RegisterUdpClient(IPEndPoint ipEndpoint, Action<ulong> callback)
        {
            lock (registrationLock)
            {

                if (ipEndpoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    HermesUtils.HermesUtils.HermesLogError("ERROR! Cannot register UDP client for IPv6! HermesUDPListener only supports IPv4!");
                    return 0;
                }

                ulong epKey = GetEndpointKey(ipEndpoint);
                ulong subKey = GetSubKey(epKey, nextId);

                udpClients.GetOrAdd(epKey, (_) => { return new UdpClient(ipEndpoint); });
                buffers.GetOrAdd(subKey, new ConcurrentQueue<UdpReceiveResult>());

                subCallbacks.GetOrAdd(subKey, callback);
                idsInUse.Add(nextId);

                // Make this more efficient somehow.
                while (idsInUse.Contains(++nextId)) nextId++;

                return subKey;
            }
        }

        public static void DeregisterUdpClient(ulong subKey)
        {
            lock (registrationLock)
            {
                buffers.TryRemove(subKey, out _);
                subCallbacks.TryRemove(subKey, out _);
                idsInUse.Remove(GetSubIdFromSubKey(subKey));

                ulong endpointKey = GetEndpointKeyFromSubKey(subKey);

                // Number of subscribers with same endpoint
                // TODO::ARGYRASPIDES() { Find a more efficient way to do this.
                // Its possible some processes could constantly register/deregister in future }
                int subsLeft = buffers.Keys.Where(_subKey => GetEndpointKeyFromSubKey(_subKey) == endpointKey).Count();

                if (subsLeft == 0) udpClients.TryRemove(endpointKey, out _);
            }
        }

        public static UdpReceiveResult Receive(ulong subKey)
        {
            if (!buffers.TryGetValue(subKey, out ConcurrentQueue<UdpReceiveResult> buff))
            {
                Console.WriteLine($"Unable to find subscriber with ID of {subKey}!");
                return new UdpReceiveResult();
            }

            buff.TryDequeue(out UdpReceiveResult res);
            return res;
        }

        // Encode Address:Port into a 64-bit unsigned number
        // Starting from most significant bits, each octet will be encoded
        // The last 16 bits are reserved for a subscriber ID
        //
        // E.g., 127.0.0.1:14550 returns:
        // (01111111) (00000000) (00000000) (00000001) (00111000 11010110) 00000000 00000000
        //    (127)  .   (0)    .   (0)    .   (1)    :      (14550)
        private static ulong GetEndpointKey(IPEndPoint ipEndpoint)
        {

            byte[] octets = ipEndpoint.Address.GetAddressBytes();
            ulong key = 0;
            for (int i = 0; i < octets.Length; i++)
            {
                key |= (ulong)octets[i] << ((octets.Length - i + 3) * 8);
            }

            key |= (ulong)ipEndpoint.Port << 16;

            return key;
        }

        // See GetEndpointKey above. Simply inserts the subscriber ID in the last 16 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetSubKey(ulong endpointKey, ushort subId)
        {
            return endpointKey | (ulong)subId;
        }

        // Extracts the first 48 bits from the subkey, which contains
        // Address:Port as 4 octets:2 octets
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetEndpointKeyFromSubKey(ulong subKey)
        {
            return subKey &= (~(ulong)ushort.MaxValue);
        }

        // Extracts the last 16 bits (subscriber ID) from a subscriber key,
        // containing Address:Port:SubID
        private static ushort GetSubIdFromSubKey(ulong subKey)
        {
            return (ushort) (subKey &= (ulong)ushort.MaxValue);
        }

        private static async void ListenUDP(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (KeyValuePair<ulong, UdpClient> client in udpClients)
                {
                    UdpReceiveResult data = await client.Value.ReceiveAsync(ct);

                    foreach (KeyValuePair<ulong, ConcurrentQueue<UdpReceiveResult>> buff in buffers)
                    {
                        if (GetEndpointKeyFromSubKey(buff.Key) != client.Key) continue;

                        if (buff.Value.Count == MAX_BUFFER_SIZE)
                        {
                            buff.Value.TryDequeue(out _);
                        }

                        buff.Value.Enqueue(data);

                        // Let subscriber know udp message has been received
                        subCallbacks.TryGetValue(buff.Key, out Action<ulong> subCallback);
                        subCallback?.Invoke(buff.Key);
                    }
                }
            }
        }

        public static void Dispose()
        {
            Console.WriteLine("Destroying HermesUDPListener ...");

            cancellationTokenSource.Cancel();

            listenerThread.Join();

            foreach (UdpClient client in udpClients.Values)
            {
                client.Close();
                client.Dispose();
            }

            Console.WriteLine("Finished destroying HermesUDPListener!");
        }
    }
