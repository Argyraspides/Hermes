using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hermes.Common.Communications.WorldListener;

public class WebSocketListener
{
    private ConcurrentDictionary<Uri, Tuple<ClientWebSocket, CancellationToken>> m_webSocketClients =
        new ConcurrentDictionary<Uri, Tuple<ClientWebSocket, CancellationToken>>();

    private Thread m_listenerThread;

    private Action<byte[]> m_onWebSocketMsgReceived;

    public WebSocketListener()
    {
    }

    public WebSocketListener(params Uri[] uris)
    {
        InitializeWebSocketClients(uris);
    }

    public void InitializeWebSocketClients(params Uri[] uris)
    {
        for (int i = 0; i < uris.Length; i++)
        {
            if (uris[i] == null)
            {
                throw new ArgumentNullException("Cannot create WebSocketListener without a valid uri");
            }

            m_webSocketClients.TryAdd(uris[i],
                new Tuple<ClientWebSocket, CancellationToken>(new ClientWebSocket(), CancellationToken.None));
        }
    }

    public void ConnectAllWebSocketClients()
    {
        foreach (KeyValuePair<Uri, Tuple<ClientWebSocket, CancellationToken>> kvp in m_webSocketClients)
        {
            Uri uri = kvp.Key;
            ClientWebSocket webSocketClient = kvp.Value.Item1;
            CancellationToken token = kvp.Value.Item2;
            TryConnectWebSocketClient(webSocketClient, uri, token);
        }
    }

    public void TryConnectWebSocketClient(ClientWebSocket webSocketClient, Uri uri, CancellationToken cancellationToken)
    {
        Task connectionTask = webSocketClient.ConnectAsync(uri, cancellationToken);
        connectionTask.Wait();
        if (connectionTask.IsCompletedSuccessfully)
        {
            Console.WriteLine($"WebSocket Successfully Connected to {uri}");
        }
    }

    public async void ReceiveWebSocketMessage(Uri uri)
    {
        ClientWebSocket webSocketClient = m_webSocketClients[uri].Item1;
        CancellationToken cancellationToken = m_webSocketClients[uri].Item2;

        byte[] buffer = new byte[1024 * 4];

        try
        {
            switch (webSocketClient.State)
            {
                case WebSocketState.Open:
                    var receiveMessageTask = await webSocketClient.ReceiveAsync(buffer, cancellationToken);
                    if (receiveMessageTask.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"WebSocket Close received from {uri}");
                    }

                    m_onWebSocketMsgReceived.Invoke(buffer);

                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket Receive Exception: {ex}");
        }


        Console.WriteLine($"WebSocket Receive message failed! On: {uri}");
    }

    public void StartListeningWebSocketClients()
    {
        while (m_listenerThread.IsAlive)
        {
            foreach (var kvp in m_webSocketClients)
            {
                ReceiveWebSocketMessage(kvp.Key);
            }
        }
    }

    public void StartListeningThread()
    {
        m_listenerThread = new Thread(StartListeningWebSocketClients);
        m_listenerThread.Start();
    }

    public void SetWebSocketReceivedCallback(Action<byte[]> onWebSocketMsgReceived)
    {
        m_onWebSocketMsgReceived = onWebSocketMsgReceived;
    }
}
