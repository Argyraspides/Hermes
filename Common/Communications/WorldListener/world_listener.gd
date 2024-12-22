extends Node

var udpServer: 	UDPServer = UDPServer.new()
var udpThread: 	Thread
var udpPeers: 	Array[PacketPeerUDP]
var port: 		int = 14550

signal udp_packet_received(udpPacket: PackedByteArray)

func _ready() -> void:
	udpServer.listen(port)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	udpServer.poll() 
	if udpServer.is_connection_available():
		
		var peer: 	PacketPeerUDP 	= udpServer.take_connection()
		var packet: PackedByteArray = peer.get_packet()
		
		# Reply so it knows we received the message.
		peer.put_packet(packet)
		
		udpPeers.append(peer)

	for i in range(0, udpPeers.size()):
		var packet: PackedByteArray = udpPeers[i].get_packet()
		udp_packet_received.emit(packet)
		
