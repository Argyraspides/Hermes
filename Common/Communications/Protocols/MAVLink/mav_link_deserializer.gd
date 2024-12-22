extends Node

var jsonTool: JSON = JSON.new()

signal mavlink_json_message_received(mavlinkJsonPacket: Variant)

func _on_world_listener_websocket_packet_received(websocketPacket: PackedByteArray) -> void:
	deserialize_mavlink_json_packet(websocketPacket)


func deserialize_mavlink_json_packet(packet: PackedByteArray) -> void:
	var mavlinkJsonPacket: Variant = jsonTool.parse_string(packet.get_string_from_utf8())
	if mavlinkJsonPacket == null: return
	mavlink_json_message_received.emit(mavlinkJsonPacket)
