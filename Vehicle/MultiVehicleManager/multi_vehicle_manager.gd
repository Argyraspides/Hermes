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
	add_vehicle(coreState)
	update_vehicle_core_state(coreState)


func add_vehicle(initState: CoreState) -> void:
	
	if m_vehicles.has(initState.m_vehicleId):
		return
	var vehicle: Node2D = m_vehicleScene.instantiate()
	m_vehicles[initState.m_vehicleId] = vehicle
	add_child(vehicle)
	
		
func update_vehicle_core_state(coreState: CoreState) -> void:
	var vehicle: Node2D = m_vehicles.get(coreState.m_vehicleId)
	
	# Vehicle scene -> m_coreState (type CoreState) -> copy_known_fields()
	vehicle.m_coreState.copy_known_fields(coreState)
	
