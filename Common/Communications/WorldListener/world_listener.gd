extends Node

# >>>>>>>>>>>>>>>> UDP >>>>>>>>>>>>>>>>
var udpServer	: 	UDPServer = UDPServer.new()
var udpThread	: 	Thread
var udpPeers	: 	Array[PacketPeerUDP]
var udpPort		: 	int = 14550

signal udp_packet_received(udpPacket: PackedByteArray)

# >>>>>>>>>>>>>>>> WS >>>>>>>>>>>>>>>>
var websocketClient		: 	WebSocketPeer = WebSocketPeer.new()
var websocketConnected	: 	bool = false
var websocketThread		: 	Thread

signal websocket_packet_received(websocketPacket: PackedByteArray)


func _ready() -> void:
	udpThread 		= Thread.new()
	udpThread.start(process_udp_packets)
	
	websocketThread = Thread.new()
	websocketThread.start(process_websocket_packets)

func process_udp_packets() -> void:
	
	udpServer.listen(udpPort)
	
	while(true):
		udpServer.poll() 
		if udpServer.is_connection_available():
			
			var peer: 	PacketPeerUDP 	= udpServer.take_connection()
			var packet: PackedByteArray = peer.get_packet()
			
			# Reply so it knows we received the message.
			peer.put_packet(packet)
			
			udpPeers.append(peer)

		for i in range(0, udpPeers.size()):
			var packet: PackedByteArray = udpPeers[i].get_packet()
			if packet.is_empty(): continue
			call_thread_safe("emit_signal", "udp_packet_received", packet)

func process_websocket_packets() -> void:
	
	var err: Error
	# TODO: Change hardcoded URL to parametrized string array global variable
	err = websocketClient.connect_to_url("ws://localhost:8765")
	
	while true:
		websocketClient.poll()
		var state: WebSocketPeer.State = websocketClient.get_ready_state()
		
		# Handle connection state
		match state:
			WebSocketPeer.STATE_OPEN:
				if !websocketConnected:
					print("Connected to server!")
					websocketConnected = true
					
				while websocketClient.get_available_packet_count():
					var packet: PackedByteArray = websocketClient.get_packet()
					if packet.is_empty(): continue
					call_thread_safe("emit_signal", "websocket_packet_received", packet)

					
			WebSocketPeer.STATE_CLOSING:
				# Keep polling to achieve proper close
				pass
				
			WebSocketPeer.STATE_CLOSED:
				var code: 	int 	= websocketClient.get_close_code()
				var reason: String 	= websocketClient.get_close_reason()
				#print("WebSocket closed with code: %d, reason %s" % [code, reason])
				websocketConnected = false
				# Attempt to reconnect
				websocketClient.connect_to_url("ws://localhost:8765")
