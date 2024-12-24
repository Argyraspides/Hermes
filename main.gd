extends Control

func _on_mav_link_state_converter_vehicle_core_state_received(coreState: CoreState) -> void:
	$MultiVehicleManager.update_all_vehicles(coreState)
