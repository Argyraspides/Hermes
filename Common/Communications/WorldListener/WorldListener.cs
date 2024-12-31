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
public partial class WorldListener : Node
{

	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> UDP <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `

	// Maps UDP Port #'s to UdpClient
	private ConcurrentDictionary<UdpClient, IPEndPoint> udpClients;
	private Thread udpThread;
	private CancellationTokenSource udpCancelTokenSrc;
	private int udpPollDelay;

	[Signal]
	public delegate void UdpPacketReceivedEventHandler(byte[] udpPacket);

	// Queue of function calls which invoke the UdpPacketReceivedEventHandler(byte[] packet) signal
	private ConcurrentQueue<Action> udpPacketReceivedSignalQueue;

	private void InitializeUdpServer()
	{
		udpClients = new ConcurrentDictionary<UdpClient, IPEndPoint>();
		udpCancelTokenSrc = new CancellationTokenSource();
		udpPacketReceivedSignalQueue = new ConcurrentQueue<Action>();
		udpPollDelay = 100; // 100ms

		// // Listen in on UDP port 14550 on any address by default (standard MAVLink port)
		// IPEndPoint defaultEndpoint = new IPEndPoint(IPAddress.Any, KnownWorlds.DEFAULT_MAVLINK_PORT);
		// udpClients.TryAdd(new UdpClient(defaultEndpoint), defaultEndpoint);

		udpThread = new Thread(ListenToUdp);
		udpThread.Start();

	}

	private void ListenToUdp()
	{
		while (!udpCancelTokenSrc.IsCancellationRequested)
		{
			foreach (KeyValuePair<UdpClient, IPEndPoint> kvp in udpClients)
			{
				UdpClient udpClient = kvp.Key;
				IPEndPoint ipEndPoint = kvp.Value;
				byte[] rawUdpData = udpClient.Receive(ref ipEndPoint);
				if (rawUdpData.IsEmpty()) continue;
				udpPacketReceivedSignalQueue.Enqueue(() => ReceivedUdpPacket(rawUdpData));
			}
			Thread.Sleep(udpPollDelay);
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
	private ConcurrentDictionary<ClientWebSocket, Uri> websocketClients;
	private CancellationTokenSource websocketCancelTokenSrc;
	private ConcurrentQueue<Action> websocketPacketReceivedSignalQueue;

	private Thread websocketThread;
	private int websocketPollDelay;

	[Signal]
	public delegate void WebSocketPacketReceivedEventHandler(byte[] websocketPacket);

	private async void InitializeWebSocketServer()
	{
		websocketClients = new ConcurrentDictionary<ClientWebSocket, Uri>();
		websocketCancelTokenSrc = new CancellationTokenSource();
		websocketPacketReceivedSignalQueue = new ConcurrentQueue<Action>();
		websocketPollDelay = 100; // 100ms

		var client = new ClientWebSocket();
		try
		{
			await AttemptConnectDefaultWebSocketWithRetries(client);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"HERMES: Failed to connect to WebSocket after all retries: {ex.Message}");
			client.Dispose();
			return;
		}

		websocketThread = new Thread(ListenToWebSockets);
		websocketThread.Start();
	}

	private async Task AttemptConnectDefaultWebSocketWithRetries(ClientWebSocket client)
	{
		int maxRetries = 3;
		int currentRetry = 0;
		int baseDelay = 1000; // Start with 1 second delay

		while (currentRetry < maxRetries)
		{
			try
			{
				await AttemptConnectDefaultWebSocket(client);
				// If we get here, connection was successful
				GD.Print($"HERMES: Successfully connected to WebSocket on attempt {currentRetry + 1}");
				return;
			}
			catch (Exception ex)
			{
				currentRetry++;
				if (currentRetry >= maxRetries)
				{
					throw; // Re-throw the last exception if we've exhausted all retries
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
			websocketCancelTokenSrc.Token
		);
		websocketClients.TryAdd(client, new Uri(KnownWorlds.DEFAULT_WEBSOCKET_URL));
	}

	private void ReceivedWebSocketPacket(byte[] websocketPacket)
	{
		EmitSignal("WebSocketPacketReceived", websocketPacket);
	}

	private async void ListenToWebSockets()
	{

		while (!websocketCancelTokenSrc.IsCancellationRequested)
		{
			foreach (KeyValuePair<ClientWebSocket, Uri> clientPair in websocketClients)
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

							websocketPacketReceivedSignalQueue.Enqueue(() => { ReceivedWebSocketPacket(packet); });
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
					websocketClients.TryRemove(clientPair);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Unexpected error: {ex.Message}");
				}
			}

			Thread.Sleep(websocketPollDelay);
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		processConcurrentQueue(udpPacketReceivedSignalQueue);
		processConcurrentQueue(websocketPacketReceivedSignalQueue);
	}

	public override void _ExitTree()
	{
		udpCancelTokenSrc.Cancel();
		websocketCancelTokenSrc.Cancel();

		udpThread.Join();
		websocketThread.Join();

		foreach (KeyValuePair<ClientWebSocket, Uri> kvp in websocketClients) kvp.Key.Dispose();
		foreach (KeyValuePair<UdpClient, IPEndPoint> kvp in udpClients) kvp.Key.Dispose();
	}

	private void processConcurrentQueue(ConcurrentQueue<Action> concurrentQueue)
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
