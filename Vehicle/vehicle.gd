extends Node2D

@onready var m_vehicleIcon: Sprite2D = $VehicleIcon

# Ew ... I don't like this verbosity ...  disgusting
var m_coreState	:  CoreState

func _ready() -> void:
	pass
	
func _process(delta: float) -> void:
	pass

func init_sprite() -> void:
	if m_coreState.m_vehicleType == VehicleTypes.VehicleType.GENERIC_MULTICOPTER:
		m_vehicleIcon.texture = load("res://Vehicle/Assets/Images/QuadcopterIcon.png")
