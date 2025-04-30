using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hermes.Common.Networking.UDP;

/// <summary>
/// We are unable to listen to the same IPEndpoint in two parts of the application at once. HermesUdpClient serves as a centralized
/// location to register and listen to UDP packets on a particular endpoint.
///
/// Example usage:
///
/// // Keep this ID as it's how you'll be able to read correct data
/// uint myId = RegisterUdpClient(new IPEndpoint.Parse("127.0.0.1", 14550));
///
/// In an async function somewhere:
///
/// byte[] data = await HermesUdpClient.ReceiveAsync(myId, new IPEndpoint.Parse("127.0.0.1", 14550));
///
///
/// </summary>
public static class HermesUdpClient
{

    private const uint MAX_BUFFER_SIZE = 512;

    private static uint nextId = 0;

    // Map the IP endpoints as strings to UdpClients (IP endpoints are the source of truth for UdpClients' identity)
    private static ConcurrentDictionary<string, UdpClient> udpClients = new ConcurrentDictionary<string, UdpClient>();

    // Map the IP endpoints to the buffers for each of the UdpClients
    private static ConcurrentDictionary<string, UdpReceiveResult[]> buffers = new ConcurrentDictionary<string, UdpReceiveResult[]>();

    // Map the subscriber IDs to their own buffer pointers (combination of ID and IPEndpoint, as one subscriber may want to listen to
    // multiple endpoints thus each read will have its own pointer)
    private static ConcurrentDictionary<string, uint> readBufferPointers = new ConcurrentDictionary<string, uint>();

    // Map the ip endpoint key to the index position of the "write head"
    private static ConcurrentDictionary<string, uint> writeBufferPointers = new ConcurrentDictionary<string, uint>();

    public static uint RegisterUdpClient(IPEndPoint ipEndpoint)
    {
        string endpointKey = GetEndpointKey(ipEndpoint);
        string bufferPointerKey = GetBufferPointerKey(nextId, ipEndpoint);

        if (!udpClients.TryGetValue(endpointKey, out _))
        {
            if (udpClients.TryAdd(endpointKey, new UdpClient(ipEndpoint)))
            {
                writeBufferPointers.TryAdd(endpointKey, 0);
                buffers.TryAdd(endpointKey, new UdpReceiveResult[MAX_BUFFER_SIZE]);
            }
        }

        readBufferPointers.GetOrAdd(bufferPointerKey, 0);

        return Interlocked.Increment(ref nextId) - 1;

    }

    public static void DeregisterUdpClient(uint id, IPEndPoint endpoint)
    {
        string bufferPointerKey = GetBufferPointerKey(id, endpoint);
        readBufferPointers.TryRemove(bufferPointerKey, out _);

        string endpointKey = GetEndpointKey(endpoint);
        bool readersStillExist = readBufferPointers.Keys
            .Any(key => key.StartsWith($"{endpointKey}:ID::"));

        if (!readersStillExist)
        {
            if (udpClients.TryRemove(endpointKey, out UdpClient client))
            {
                client.Dispose();
            }
            buffers.TryRemove(endpointKey, out _);
        }
    }

    public static async Task<UdpReceiveResult> ReceiveAsync(uint id, IPEndPoint ipEndpoint)
    {
        string endpointKey = GetEndpointKey(ipEndpoint);
        string bufferPointerKey = GetBufferPointerKey(id, ipEndpoint);

        ValidateKeys(endpointKey, bufferPointerKey);

        UdpReceiveResult dat = await udpClients[endpointKey].ReceiveAsync();

        AddToBuffer(endpointKey, dat);

        uint currentReadPosition = 0;
        readBufferPointers.AddOrUpdate(
            bufferPointerKey,
            0,
            (key, currentValue) => {
                currentReadPosition = currentValue;
                return (currentValue + 1) % MAX_BUFFER_SIZE;
            }
        );

        return buffers[endpointKey][currentReadPosition];
    }

    private static void ValidateKeys(string endpointKey, string bufferPointerKey)
    {
        if (string.IsNullOrEmpty(endpointKey) || string.IsNullOrEmpty(bufferPointerKey))
        {
            throw new NoNullAllowedException("No endpoint key or buffer pointer");
        }

        if (!udpClients.TryGetValue(endpointKey, out _))
        {
            throw new KeyNotFoundException($"Cannot find UdpClient associated with IP Endpoint: {endpointKey}");
        }

        if (!readBufferPointers.TryGetValue(bufferPointerKey, out _))
        {
            throw new KeyNotFoundException(
                $"Cannot find buffer pointer for subscriber with buffer pointer key: {bufferPointerKey} and IP endpoint: {endpointKey}");
        }

        if (!buffers.TryGetValue(endpointKey, out _))
        {
            throw new KeyNotFoundException($"Cannot find buffer for UdpClient with IP endpoint: {endpointKey}");
        }

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

    private static string GetBufferPointerKey(uint id, IPEndPoint ipEndpoint)
    {
        return $"{GetEndpointKey(ipEndpoint)}:ID::{id}";
    }

    private static void AddToBuffer(string endpointKey, UdpReceiveResult res)
    {

        uint writePosition = 0;
        writeBufferPointers.AddOrUpdate(
            endpointKey,
            0,
            (key, currentValue) => {
                writePosition = currentValue;
                return (currentValue + 1) % MAX_BUFFER_SIZE;
            }
        );

        buffers[endpointKey][writePosition] = res;
    }

}
