extends Node
class_name MAVLinkReassembler

# Constants for packet handling
const MAX_PACKET_BUFFER_SIZE: int = 256  # Maximum number of packet fragments to store
const PACKET_TIMEOUT_MS: int = 1000      # Time to wait for missing fragments before dropping
const MAX_SEQUENCE: int = 255            # Maximum sequence number before wrapping

# Signal emitted when a complete message is reassembled
signal message_reassembled(complete_packet: PackedByteArray)

# Structure to track fragment information
class FragmentInfo extends RefCounted:
	var fragments: Dictionary  	# Dictionary[int, PackedByteArray]  # offset -> fragment data
	var last_update: int      	# Time of last fragment receipt
	var expected_length: int  	# Total expected message length
	var msg_id: int          	# Message ID for this set of fragments
	var sysid: int          	# System ID
	var compid: int         	# Component ID
	
	func _init(p_msg_id: int, p_sysid: int, p_compid: int, p_expected_length: int) -> void:
		fragments = {}
		last_update = Time.get_ticks_msec()
		expected_length = p_expected_length
		msg_id = p_msg_id
		sysid = p_sysid
		compid = p_compid

# Map of message identifiers to their fragment information
# Key format: "{sysid}_{compid}_{msg_id}"
var _fragment_buffers: Dictionary = {}  # Dictionary[String, FragmentInfo]

func _ready() -> void:
	# Start the cleanup timer
	var timer: Timer = Timer.new()
	timer.wait_time = 1.0  # Check every second
	timer.timeout.connect(_cleanup_old_fragments)
	add_child(timer)
	timer.start()

func _cleanup_old_fragments() -> void:
	var current_time: int = Time.get_ticks_msec()
	var keys_to_remove: Array[String] = []
	
	# Identify old fragment sets
	for key: String in _fragment_buffers:
		var fragment_info: FragmentInfo = _fragment_buffers[key]
		if current_time - fragment_info.last_update > PACKET_TIMEOUT_MS:
			keys_to_remove.append(key)
	
	# Remove expired fragment sets
	for key: String in keys_to_remove:
		_fragment_buffers.erase(key)

func _get_buffer_key(sysid: int, compid: int, msg_id: int) -> String:
	return "%d_%d_%d" % [sysid, compid, msg_id]

func process_packet(packet: PackedByteArray) -> void:
	# Extract basic header information
	var magic: int = packet[0]
	if magic != 0xFD && magic != 0xFE:  # Only handle MAVLink v2 for now
		push_error("Only MAVLink v2 packets are supported")
		return
	
	var len: int = packet[1]
	var seq: int = packet[4]
	var sysid: int = packet[5]
	var compid: int = packet[6]
	var msgid: int = (packet[9] << 16) | (packet[8] << 8) | packet[7]
	
	# Check if this is a fragmented message by looking at incompat_flags
	var incompat_flags: int = packet[2]
	var is_signed: bool = (incompat_flags & 0x01) != 0
	var has_fragmentation: bool = (incompat_flags & 0x02) != 0
	
	if not has_fragmentation:
		# If not fragmented, emit the packet directly
		message_reassembled.emit(packet)
		return
	
	# Extract fragmentation information from the packet
	# Fragment information is stored after the standard header (10 bytes)
	# and before the payload
	var fragment_offset: int = _extract_uint16(packet, 10)
	var fragment_size: int = len
	var total_size: int = _extract_uint16(packet, 12)
	
	# Get or create fragment info
	var buffer_key: String = _get_buffer_key(sysid, compid, msgid)
	var fragment_info: FragmentInfo
	
	if not _fragment_buffers.has(buffer_key):
		fragment_info = FragmentInfo.new(msgid, sysid, compid, total_size)
		_fragment_buffers[buffer_key] = fragment_info
	else:
		fragment_info = _fragment_buffers[buffer_key]
		fragment_info.last_update = Time.get_ticks_msec()
	
	# Store the fragment
	# Skip the fragmentation info (4 bytes) when storing the payload
	var payload_start: int = 14  # 10 (header) + 4 (frag info)
	var fragment_data: PackedByteArray = packet.slice(payload_start, payload_start + fragment_size)
	fragment_info.fragments[fragment_offset] = fragment_data
	
	# Check if we have all fragments
	if _is_message_complete(fragment_info):
		var complete_message: PackedByteArray = _reassemble_message(fragment_info)
		if complete_message != null:
			message_reassembled.emit(complete_message)
		_fragment_buffers.erase(buffer_key)

func _extract_uint16(packet: PackedByteArray, offset: int) -> int:
	return (packet[offset + 1] << 8) | packet[offset]

func _is_message_complete(fragment_info: FragmentInfo) -> bool:
	var total_length: int = 0
	var expected_offset: int = 0
	
	# Sort fragment offsets to ensure proper order
	var offsets: Array = fragment_info.fragments.keys()
	offsets.sort()
	
	# Check for continuity and completeness
	for offset: int in offsets:
		if offset != expected_offset:
			return false
		var fragment: PackedByteArray = fragment_info.fragments[offset]
		total_length += fragment.size()
		expected_offset += fragment.size()
	
	return total_length == fragment_info.expected_length

func _reassemble_message(fragment_info: FragmentInfo) -> PackedByteArray:
	var complete_payload: PackedByteArray = PackedByteArray()
	var offsets: Array = fragment_info.fragments.keys()
	offsets.sort()
	
	# Concatenate fragments in order
	for offset: int in offsets:
		complete_payload.append_array(fragment_info.fragments[offset])
	
	# Create new MAVLink message with reassembled payload
	var message: PackedByteArray = PackedByteArray()
	message.append(0xFD)  # magic
	message.append(complete_payload.size())  # len
	message.append(0)  # incompat_flags (clear fragmentation flag)
	message.append(0)  # compat_flags
	message.append(0)  # seq
	message.append(fragment_info.sysid)  # sysid
	message.append(fragment_info.compid)  # compid
	
	# Message ID (3 bytes, little endian)
	message.append(fragment_info.msg_id & 0xFF)
	message.append((fragment_info.msg_id >> 8) & 0xFF)
	message.append((fragment_info.msg_id >> 16) & 0xFF)
	
	# Add reassembled payload
	message.append_array(complete_payload)
	
	# Calculate and append checksum
	var crc: int = _calculate_checksum(message)
	message.append(crc & 0xFF)
	message.append((crc >> 8) & 0xFF)
	
	return message

func _calculate_checksum(message: PackedByteArray) -> int:
	# Implementation of CRC-16/MCRF4XX for MAVLink checksum
	var crc: int = 0xFFFF
	
	# Process all bytes except the magic byte
	for i in range(1, message.size()):
		var tmp: int = message[i] ^ (crc & 0xFF)
		tmp = (tmp ^ (tmp << 4)) & 0xFF
		crc = (crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4)
		crc = crc & 0xFFFF  # Ensure 16-bit value
	
	return crc

func _on_world_listener_udp_packet_received(udpPacket: PackedByteArray) -> void:
	if !udpPacket.is_empty():
		process_packet(udpPacket)
