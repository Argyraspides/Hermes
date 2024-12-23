extends Node

var jsonTool: JSON = JSON.new()

signal mavlink_json_message_received(mavlinkJsonPacket: Variant)

func _on_world_listener_websocket_packet_received(websocketPacket: PackedByteArray) -> void:
	deserialize_mavlink_json_packet(websocketPacket)


func deserialize_mavlink_json_packet(packet: PackedByteArray) -> void:
	if packet == null || packet.is_empty(): return
	
	var packetStr: String = packet.get_string_from_utf8()
	packetStr = packetStr.replace("NaN", "null")
	var mavlinkJsonPacket: Variant = jsonTool.parse_string(packetStr)
	if mavlinkJsonPacket == null: return
	mavlink_json_message_received.emit(mavlinkJsonPacket)
