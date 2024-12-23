extends Node


var m_vehicles: Dictionary = {}
@export var m_vehicleScene: PackedScene

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func _on_message_broker_vehicle_core_state_received(coreState: CoreState) -> void:
	pass # Replace with function body.


func add_vehicle(vehicleId: int, initState: CoreState) -> void:
	
	if m_vehicles.has(vehicleId):
		push_warning("Vehicle with ID %s already exists" % vehicleId)
	
		
func update_vehicle_core_state(vehicleId: int, coreState: CoreState) -> void:
	var vehicle: PackedScene = m_vehicles.get(vehicleId)
	
	# Vehicle scene -> m_coreState (type CoreState) -> copy_known_fields()
	vehicle.m_coreState.copy_known_fields(coreState)
