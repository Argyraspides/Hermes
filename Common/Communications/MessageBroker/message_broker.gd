extends Node

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

signal vehicle_core_state_received(coreState: CoreState)
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func _on_mav_link_state_converter_vehicle_core_state_received(coreState: CoreState) -> void:
	vehicle_core_state_received.emit(coreState)
