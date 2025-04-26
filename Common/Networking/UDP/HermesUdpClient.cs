using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private const uint MAX_BUFFER_SIZE = 4096;

    private static uint nextId = 0;

    // Map the IP endpoints as strings to UdpClients (IP endpoints are the source of truth for UdpClients' identity)
    private static ConcurrentDictionary<string, UdpClient> udpClients = new ConcurrentDictionary<string, UdpClient>();

    // Map the IP endpoints to the buffers for each of the UdpClients
    private static ConcurrentDictionary<string, byte[][]> buffers = new ConcurrentDictionary<string, byte[][]>();

    // Map the subscriber IDs to their own buffer pointers (combination of ID and IPEndpoint, as one subscriber may want to listen to
    // multiple endpoints thus each read will have its own pointer)
    private static ConcurrentDictionary<string, uint> readBufferPointers = new ConcurrentDictionary<string, uint>();

    // Map the ip endpoint key to the index position of the "write head"
    private static ConcurrentDictionary<string, uint> writeBufferPointers = new ConcurrentDictionary<string, uint>();


    public static uint RegisterUdpClient(IPEndPoint ipEndpoint)
    {

        // Atomically get and increment our ID first to prevent race conditions
        // (in this case we would give two threads the same ID)
        uint id = Interlocked.Increment(ref nextId) - 1;

        string endpointKey = GetEndpointKey(ipEndpoint);
        string bufferPointerKey = GetBufferPointerKey(id, ipEndpoint);

        udpClients.GetOrAdd(endpointKey, key => new UdpClient(ipEndpoint));
        writeBufferPointers.GetOrAdd(endpointKey, key => 0);

        readBufferPointers.GetOrAdd(bufferPointerKey, 0);
        buffers.GetOrAdd(endpointKey, new byte[MAX_BUFFER_SIZE][]);

        return id;
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

    public static async Task<byte[]> ReceiveAsync(uint id, IPEndPoint ipEndpoint)
    {
        string endpointKey = GetEndpointKey(ipEndpoint);
        string bufferPointerKey = GetBufferPointerKey(id, ipEndpoint);
        if (!udpClients.TryGetValue(endpointKey, out UdpClient client))
        {
            throw new KeyNotFoundException($"Cannot find UdpClient associated with IP Endpoint: {endpointKey}");
        }

        if (!readBufferPointers.TryGetValue(bufferPointerKey, out _))
        {
            throw new KeyNotFoundException(
                $"Cannot find buffer pointer for subscriber with ID: {id} and IP endpoint: {endpointKey}");
        }

        if (!buffers.TryGetValue(endpointKey, out _))
        {
            throw new KeyNotFoundException($"Cannot find buffer for UdpClient with IP endpoint: {endpointKey}");
        }

        var dat = await client.ReceiveAsync();
        AddToBuffer(endpointKey, dat.Buffer);

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

    private static string GetEndpointKey(IPEndPoint ipEndpoint)
    {
        return $"{ipEndpoint.Address}:{ipEndpoint.Port}";
    }

    private static string GetBufferPointerKey(uint id, IPEndPoint ipEndpoint)
    {
        return $"{GetEndpointKey(ipEndpoint)}:ID::{id}";
    }

    private static void AddToBuffer(string endpointKey, byte[] buffer)
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

        buffers[endpointKey][writePosition] = buffer;
    }

}
