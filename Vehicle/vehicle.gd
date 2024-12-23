extends Node2D

@onready var 	m_vehicleIcon		:	Sprite2D = $VehicleIcon
var 			m_vehicleIconSet	:	bool = false

@onready var 	m_coreState			:  	CoreState = CoreState.new()

func _process(delta: float) -> void:
	set_sprite()
	
func set_sprite() -> void:
	if m_vehicleIconSet: return
	if m_coreState.m_vehicleType == VehicleTypes.VehicleType.GENERIC_QUADCOPTER:
		m_vehicleIcon.texture = load("res://Vehicle/Assets/Images/QuadcopterIcon.png")
		m_vehicleIconSet = true

func update_position() -> void:
	pass
