class_name MAVLinkMessage

var magic			: 	int              	# Protocol magic marker (0xFD for v2)
var len				: 	int               	# Length of payload
var incompat_flags	: 	int    				# Incompatibility flags
var compat_flags	: 	int      			# Compatibility flags
var seq				: 	int              	# Sequence number
var sysid			: 	int            		# System ID
var compid			: 	int           		# Component ID
var msgid			: 	int            		# Message ID (24 bit)
var payload			: 	PackedByteArray  	# Message payload
var checksum		: 	int         		# Message checksum
	
func _init(
	p_magic			: 	int = 0xFD,
	p_len			: 	int = 0,
	p_incompat_flags: 	int = 0,
	p_compat_flags	: 	int = 0,
	p_seq			: 	int = 0,
	p_sysid			: 	int = 0,
	p_compid		: 	int = 0,
	p_msgid			: 	int = 0,
	p_payload		: 	PackedByteArray = PackedByteArray(),
	p_checksum		: 	int = 0
) -> void:
	
	magic 			= 	p_magic
	len 			= 	p_len
	incompat_flags 	= 	p_incompat_flags
	compat_flags 	= 	p_compat_flags
	seq 			= 	p_seq
	sysid 			= 	p_sysid
	compid 			= 	p_compid
	msgid 			= 	p_msgid
	payload 		= 	p_payload
	checksum 		= 	p_checksum

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
