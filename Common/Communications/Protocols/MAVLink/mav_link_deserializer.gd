extends Node
class_name MAVLinkDeserializer

const MAVLINK_V1_MAGIC_NUMBER: int = 0xFE
const MAVLINK_V2_MAGIC_NUMBER: int = 0xFD
static var mavlink_message_map: MAVLinkMessageMap = MAVLinkMessageMap.new()

const TYPE_SIZES: Dictionary = {
	"uint8_t"	: 	1,
	"int8_t"	: 	1,
	"uint16_t"	: 	2,
	"int16_t"	: 	2,
	"uint32_t"	: 	4,
	"int32_t"	: 	4,
	"uint64_t"	: 	8,
	"int64_t"	: 	8,
	"float"		: 	4,
	"double"	: 	8,
	"char"		: 	1
}

signal mavlink_message_struct_processed(mavlink_message: MAVLinkMessage)

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
	
	
func make_mavlink_message_struct(packet: PackedByteArray) -> MAVLinkMessage:
	
	var mavlink_message: MAVLinkMessage = MAVLinkMessage.new()
	
	if		is_mavlink_v2(packet): 	mavlink_message = make_mavlink_v2_message_struct(packet)
	elif 	is_mavlink_v1(packet):	mavlink_message = make_mavlink_v1_message_struct(packet)
#		
	return mavlink_message
	
	
func is_mavlink_v1(packet: PackedByteArray) -> bool:
	return packet[0] == MAVLINK_V1_MAGIC_NUMBER


func is_mavlink_v2(packet: PackedByteArray) -> bool:
	return packet[0] == MAVLINK_V2_MAGIC_NUMBER
	
	
func make_mavlink_v2_message_struct(packet: PackedByteArray) -> MAVLinkMessage:
	
	var mavlink_message: MAVLinkMessage = MAVLinkMessage.new()
	
	mavlink_message.magic 			= packet[0]
	mavlink_message.len 			= packet[1]
	mavlink_message.incompat_flags 	= packet[2]
	mavlink_message.compat_flags 	= packet[3]
	mavlink_message.seq 			= packet[4]
	mavlink_message.sysid 			= packet[5]
	mavlink_message.compid 			= packet[6]

	# Message ID (24 bits split across 3 bytes)
	mavlink_message.msgid 			= (packet[9] << 16) | (packet[8] << 8) | packet[7]

	# Payload (variable length based on len field)
	mavlink_message.payload 		= packet.slice(10, 10 + mavlink_message.len)

	# Checksum (2 bytes after payload)
	var checksum_start: int 		= 10 + mavlink_message.len
	# Remember that the multi-byte checksum comes in as a little-endian format,
	# so we shift the most significant byte up during construction
	mavlink_message.checksum 		= (packet[checksum_start + 1] << 8) | packet[checksum_start]

	# Signature (13 bytes, optional)
	if packet.size() > checksum_start + 2:  # If signature is present
		var signature_start: int 	= checksum_start + 2
		mavlink_message.signature 	= packet.slice(signature_start, signature_start + 13)

	return mavlink_message
	

func make_mavlink_v1_message_struct(packet: PackedByteArray) -> MAVLinkMessage:
	
	var mavlink_message: MAVLinkMessage = MAVLinkMessage.new()
	
	mavlink_message.magic 			= packet[0] 
	mavlink_message.len 			= packet[1] 
	mavlink_message.seq 			= packet[2] 
	mavlink_message.sysid 			= packet[3] 
	mavlink_message.compid 			= packet[4] 
	mavlink_message.msgid			= packet[5]
	
	mavlink_message.payload 		= packet.slice(6, 6 + mavlink_message.len)

	# Checksum (2 bytes after payload)
	var checksum_start: int 		= 6 + mavlink_message.len
	# Remember that the multi-byte checksum comes in as a little-endian format,
	# so we shift the most significant byte up during construction
	mavlink_message.checksum 		= (packet[checksum_start + 1] << 8) | packet[checksum_start]

	return mavlink_message


func _on_mav_link_reassembler_message_reassembled(complete_packet: PackedByteArray) -> void:
	var mavlink_message: MAVLinkMessage = make_mavlink_message_struct(complete_packet)
	var deserialized_message: Object = deserialize_message(mavlink_message)	
	var hi: int = 5
	
	
# Function to convert MAVLinkMessage to specific message type
static func deserialize_message(mavlink_msg: MAVLinkMessage) -> Object:
	# Get the message class from the message map
	var msg_map: MAVLinkMessageMap = mavlink_message_map
	var msg_class: Variant = msg_map.class_map.get(mavlink_msg.msgid)
	var msg_id: int = mavlink_msg.msgid
	if not msg_class:
		push_error("Unknown message ID: " + str(mavlink_msg.msgid))
		return null
		
	# Create an instance of the specific message type
	var specific_msg: Object = msg_class.new()
	
	var offset: int = 0
	var payload: PackedByteArray = mavlink_msg.payload
	var payload_len: int = payload.size()
	
	# Iterate through fields in order using the FIELD_TYPES constant
	var field_types: Dictionary = specific_msg.get("FIELD_TYPES")
	for field_name: String in field_types:
		var field_info: Dictionary = field_types[field_name]
		var mavlink_type: String = field_info["type"]
		
		# Calculate size of this field
		var field_size: int = TYPE_SIZES[mavlink_type]
		if field_info.has("array_length"):
			field_size *= field_info["array_length"]
			
		# Check if this is an extension field and if we have enough bytes
		if field_info.get("extension", false) and offset + field_size > payload_len:
			# This is an extension field and we don't have enough bytes
			# Set default value based on type
			specific_msg.set(field_name, _get_default_value(mavlink_type, field_info))
			continue
			
		# Check if we have enough bytes for non-extension field
		if offset + field_size > payload_len:
			push_error("Payload too short for field: " + field_name)
			return null
			
		# Handle array types
		if field_info.has("array_length"):
			var array_length: int = field_info["array_length"]
			var array_data: Array = []
			
			for i in range(array_length):
				var value: Variant = _extract_value(payload, offset, mavlink_type)
				array_data.append(value)
				offset += TYPE_SIZES[mavlink_type]
			
			specific_msg.set(field_name, array_data)
		else:
			# Handle single values
			var value: Variant = _extract_value(payload, offset, mavlink_type)
			specific_msg.set(field_name, value)
			offset += TYPE_SIZES[mavlink_type]
	
	return specific_msg

# Helper function to get default values for extension fields
static func _get_default_value(type: String, field_info: Dictionary) -> Variant:
	match type:
		"uint8_t", "uint16_t", "uint32_t", "uint64_t", \
		"int8_t", "int16_t", "int32_t", "int64_t":
			return 0
		"float", "double":
			return 0.0
		"char":
			return ""
		_:
			if field_info.has("array_length"):
				# Return empty array of appropriate size
				var arr: Array = []
				arr.resize(field_info["array_length"])
				return arr
			return null

# Helper function to extract values from the byte array
static func _extract_value(payload: PackedByteArray, offset: int, type: String) -> Variant:
	match type:
		"uint8_t":
			return payload[offset]
		"int8_t":
			var unsigned: int = payload[offset]
			return (unsigned & 0x7F) - (unsigned & 0x80)
		"uint16_t":
			return (payload[offset + 1] << 8) | payload[offset]
		"int16_t":
			var unsigned: int = (payload[offset + 1] << 8) | payload[offset]
			return (unsigned & 0x7FFF) - (unsigned & 0x8000)
		"uint32_t":
			return (payload[offset + 3] << 24) | (payload[offset + 2] << 16) | \
				   (payload[offset + 1] << 8) | payload[offset]
		"int32_t":
			var unsigned: int = (payload[offset + 3] << 24) | (payload[offset + 2] << 16) | \
							   (payload[offset + 1] << 8) | payload[offset]
			return (unsigned & 0x7FFFFFFF) - (unsigned & 0x80000000)
		"uint64_t":
			# For uint64, we return the lower 53 bits as that's what JavaScript/GDScript can handle
			var hi: int = (payload[offset + 7] << 24) | (payload[offset + 6] << 16) | \
						 (payload[offset + 5] << 8) | payload[offset + 4]
			var lo: int = (payload[offset + 3] << 24) | (payload[offset + 2] << 16) | \
						 (payload[offset + 1] << 8) | payload[offset]
			# Limit to 53 bits of precision
			return (hi & 0x1FFFFF) * 0x100000000 + lo
		"int64_t":
			# For int64, we return the lower 53 bits as that's what JavaScript/GDScript can handle
			var hi: int = (payload[offset + 7] << 24) | (payload[offset + 6] << 16) | \
						 (payload[offset + 5] << 8) | payload[offset + 4]
			var lo: int = (payload[offset + 3] << 24) | (payload[offset + 2] << 16) | \
						 (payload[offset + 1] << 8) | payload[offset]
			var unsigned: int = (hi & 0x1FFFFF) * 0x100000000 + lo
			return (unsigned & 0x1FFFFFFFFFFFFF) - (unsigned & 0x20000000000000)
		"float":
			var bytes: PackedByteArray = PackedByteArray([
				payload[offset],
				payload[offset + 1],
				payload[offset + 2],
				payload[offset + 3]
			])
			return bytes.decode_float(0)
		"double":
			var bytes: PackedByteArray = PackedByteArray([
				payload[offset],
				payload[offset + 1],
				payload[offset + 2],
				payload[offset + 3],
				payload[offset + 4],
				payload[offset + 5],
				payload[offset + 6],
				payload[offset + 7]
			])
			return bytes.decode_double(0)
		"char":
			return char(payload[offset])
		_:
			push_error("Unknown type: " + type)
			return null
