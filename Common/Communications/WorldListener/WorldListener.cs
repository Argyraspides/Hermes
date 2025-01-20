/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;


// The WorldListener does nothing but listen to any data from the outside world
// such as UDP packets, TCP packets, WebSocket packets, etc., and emits signals
// that other components can listen to if they are interested in any such packets.
//
// In the context of Hermes, It is important that you make sure WorldListener.cs only enters the scene tree
// once the ExternalLauncher.cs has successfully launched any external files.
// This is because at the moment, MAVLink messages are listened and deserialized
// via an external Python script with MAVSDK, and sent over a WebSocket.
// If a successful connection is to be established, then the ExternalLauncher.cs
// must finish executing the Python script which starts this WebSocket server
//
//
// This means you should have a node with the WorldListener.cs script attached at a higher
// level than the ExternalLauncher.cs. Everything under Communications/ should be
// in the same scene tree.
//
// Since the WorldListener module is essentially just a network interface, it is registered as
// an Autoload (Singleton) in Hermes.
public partial class WorldListener : Node
{

	// Ensure other C# scripts can access this singleton without requiring
	// "GetNode()".
	public static WorldListener Instance { get; private set; }

	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> UDP <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `

	// Maps UDP Port #'s to UdpClient
	private ConcurrentDictionary<UdpClient, IPEndPoint> m_udpClients;
	private Thread m_udpThread;
	private CancellationTokenSource m_udpCancelTokenSrc;
	private int m_udpPollDelay;

	[Signal]
	public delegate void UdpPacketReceivedEventHandler(byte[] udpPacket);

	// Queue of function calls which invoke the UdpPacketReceivedEventHandler(byte[] packet) signal
	private ConcurrentQueue<Action> udpPacketReceivedSignalQueue;

	private void InitializeUdpServer()
	{
		m_udpClients = new ConcurrentDictionary<UdpClient, IPEndPoint>();
		m_udpCancelTokenSrc = new CancellationTokenSource();
		udpPacketReceivedSignalQueue = new ConcurrentQueue<Action>();
		m_udpPollDelay = 100; // 100ms

		// // Listen in on UDP port 14550 on any address by default (standard MAVLink port)
		// IPEndPoint defaultEndpoint = new IPEndPoint(IPAddress.Any, KnownWorlds.DEFAULT_MAVLINK_PORT);
		// udpClients.TryAdd(new UdpClient(defaultEndpoint), defaultEndpoint);

		m_udpThread = new Thread(ListenToUdp);
		m_udpThread.Start();

	}

	private void ListenToUdp()
	{
		while (!m_udpCancelTokenSrc.IsCancellationRequested)
		{
			foreach (KeyValuePair<UdpClient, IPEndPoint> kvp in m_udpClients)
			{
				UdpClient udpClient = kvp.Key;
				IPEndPoint ipEndPoint = kvp.Value;
				byte[] rawUdpData = udpClient.Receive(ref ipEndPoint);
				if (rawUdpData.IsEmpty()) continue;
				udpPacketReceivedSignalQueue.Enqueue(() => ReceivedUdpPacket(rawUdpData));
			}
			Thread.Sleep(m_udpPollDelay);
		}
	}

	private void ReceivedUdpPacket(byte[] udpPacket)
	{
		EmitSignal("UdpPacketReceived", udpPacket);
	}

	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
	//	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< UDP >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> WEBSOCKET <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
	private ConcurrentDictionary<ClientWebSocket, Uri> m_websocketClients;
	private CancellationTokenSource m_websocketCancelTokenSrc;
	private ConcurrentQueue<Action> m_websocketPacketReceivedSignalQueue;

	private Thread m_websocketThread;
	private int m_websocketPollDelay;

	[Signal]
	public delegate void WebSocketPacketReceivedEventHandler(byte[] websocketPacket);

	private async void InitializeWebSocketServer()
	{
		m_websocketClients = new ConcurrentDictionary<ClientWebSocket, Uri>();
		m_websocketCancelTokenSrc = new CancellationTokenSource();
		m_websocketPacketReceivedSignalQueue = new ConcurrentQueue<Action>();
		m_websocketPollDelay = 100; // 100ms

		try
		{
			await AttemptConnectDefaultWebSocketWithRetries();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"HERMES: Failed to connect to WebSocket after all retries: {ex.Message}");
			return;
		}

		m_websocketThread = new Thread(ListenToWebSockets);
		m_websocketThread.Start();
	}

	private async Task AttemptConnectDefaultWebSocketWithRetries()
	{
		int maxRetries = 4;
		int currentRetry = 0;
		int baseDelay = 1000; // 1000ms

		while (currentRetry < maxRetries)
		{
			ClientWebSocket client = new ClientWebSocket();
			try
			{
				await AttemptConnectDefaultWebSocket(client);
				m_websocketClients.TryAdd(client, new Uri(KnownWorlds.DEFAULT_WEBSOCKET_URL));
				GD.Print($"HERMES: Successfully connected to WebSocket on attempt {currentRetry + 1}, URL: {KnownWorlds.DEFAULT_WEBSOCKET_URL}");
				return;
			}
			catch (Exception ex)
			{
				currentRetry++;
				if (currentRetry >= maxRetries)
				{
					throw;
				}

				// Calculate delay with exponential backoff: 1s, 2s, 4s
				int delayMs = baseDelay * (int)Math.Pow(2, currentRetry - 1);
				GD.PrintErr($"HERMES: Failed to connect to WebSocket (attempt {currentRetry}/{maxRetries}): {ex.Message}");
				GD.PrintErr($"HERMES: Retrying in {delayMs / 1000} seconds...");

				await Task.Delay(delayMs);
			}
		}
	}

	private async Task AttemptConnectDefaultWebSocket(ClientWebSocket client)
	{
		await client.ConnectAsync(
			new Uri(KnownWorlds.DEFAULT_WEBSOCKET_URL),
			m_websocketCancelTokenSrc.Token
		);
	}

	private void ReceivedWebSocketPacket(byte[] websocketPacket)
	{
		EmitSignal("WebSocketPacketReceived", websocketPacket);
	}

	private async void ListenToWebSockets()
	{

		while (!m_websocketCancelTokenSrc.IsCancellationRequested)
		{
			foreach (KeyValuePair<ClientWebSocket, Uri> clientPair in m_websocketClients)
			{
				ClientWebSocket client = clientPair.Key;

				byte[] buffer = new byte[1024 * 4];

				try
				{
					if (client.State == WebSocketState.Open)
					{
						using var timeoutToken = new CancellationTokenSource(5000);

						WebSocketReceiveResult result = await client.ReceiveAsync(
							new ArraySegment<byte>(buffer),
							CancellationToken.None
						);

						var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

						if (result.Count > 0)
						{
							byte[] packet = new byte[result.Count];
							Array.Copy(buffer, packet, result.Count);

							m_websocketPacketReceivedSignalQueue.Enqueue(() => { ReceivedWebSocketPacket(packet); });
						}
					}
					if (client.State == WebSocketState.CloseSent)
					{
					}
					if (client.State == WebSocketState.Closed || client.State == WebSocketState.CloseReceived)
					{
					}
				}
				catch (OperationCanceledException)
				{
					continue;
				}
				catch (WebSocketException ex)
				{
					GD.PrintErr($"WebSocket error: {ex.Message}");
					m_websocketClients.TryRemove(clientPair);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Unexpected error: {ex.Message}");
				}
			}

			Thread.Sleep(m_websocketPollDelay);
		}
	}



	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
	//	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< WEBSOCKET >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> GODOT <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializeUdpServer();
		InitializeWebSocketServer();

		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ProcessConcurrentQueue(udpPacketReceivedSignalQueue);
		ProcessConcurrentQueue(m_websocketPacketReceivedSignalQueue);
	}

	public override void _ExitTree()
	{
		m_udpCancelTokenSrc.Cancel();
		m_websocketCancelTokenSrc.Cancel();

		m_udpThread.Join();
		m_websocketThread.Join();

		foreach (KeyValuePair<ClientWebSocket, Uri> kvp in m_websocketClients) kvp.Key.Dispose();
		foreach (KeyValuePair<UdpClient, IPEndPoint> kvp in m_udpClients) kvp.Key.Dispose();
	}

	private void ProcessConcurrentQueue(ConcurrentQueue<Action> concurrentQueue)
	{
		if (concurrentQueue.IsEmpty) return;
		while (concurrentQueue.TryDequeue(out Action action))
		{
			action?.Invoke();
		}
	}


	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
	//	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< GODOT >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

}
