## This class' sole purpose is to take a MAVLink message and convert it into an in-program
## vehicle state representation. 
##
## As an example, it can take a MAVLink message in a JSON format,
## defined as:
##	{
##		"type": "GLOBAL_POSITION_INT",
##		"msgid": 33,
##		"timestamp": "2024-12-23T13:25:24.557992",
##		"time_boot_ms": 7048,
##		"lat": 473979712,
##		"lon": 85461636,
##		"alt": 475,
##		"relative_alt": -15,
##		"vx": 0,
##		"vy": -1,
##		"vz": -2,
##		"hdg": 9126
##	}
## And convert it into an in-vehicle state. Since this is a state regarding a vehicle's position,
## which is a core part of any vehicle regardless of what it is, it will be converted into a CoreState.

extends Node
class_name MAVLinkStateConverter


# >>>>>>>>>>>>>>>> SIGNALS >>>>>>>>>>>>>>>>



## Signal that a core state has been successfully converted to from a MAVLink message
signal vehicle_core_state_received(coreState: CoreState)



# >>>>>>>>>>>>>>>> CONVERSION FUNCTIONS >>>>>>>>>>>>>>>>



## Converts a MAVLink JSON message into a core vehicle state.
## A core vehicle state is a state that applies to all vehicles regardless
## of what it does/doesn't have, e.g., a position, a velocity, an acceleration, etc.
func convert_mavlink_json_to_core_state(mavlinkJsonMessage: Variant) -> CoreState:
	var coreState: CoreState = CoreState.new()
	
	var msgid: int = mavlinkJsonMessage["msgid"]
	
	match msgid:
		0:
			var vehicleType: String = mavlinkJsonMessage["type"]
			
			match vehicleType:
				"MAV_TYPE_QUADROTOR":
					coreState.m_vehicleType = VehicleTypes.VehicleType.GENERIC_QUADCOPTER
			
		33:
			coreState.m_earthPosition.x = mavlinkJsonMessage["lat"]
			coreState.m_earthPosition.y = mavlinkJsonMessage["lon"]
			coreState.m_earthPosition.z = mavlinkJsonMessage["alt"]
			
			# Convert from MAVLink cm/s to m/s
			coreState.m_groundVel.x		= mavlinkJsonMessage["vx"] / 100.0
			coreState.m_groundVel.y		= mavlinkJsonMessage["vy"] / 100.0
			coreState.m_groundVel.z		= mavlinkJsonMessage["vz"] / 100.0
			
			coreState.m_earthHeading	= mavlinkJsonMessage["hdg"]

	return coreState
	
	
	
# >>>>>>>>>>>>>>>> RECEIVED MAVLINK MESSAGE HANDLERS >>>>>>>>>>>>>>>>



## JSON message has been received from the MAVLink deserializer
## Here we convert the MAVLink message to a state and emit the appropriate signal 
## that a state has been created
func _on_mav_link_deserializer_mavlink_json_message_received(mavlinkJsonPacket: Variant) -> void:
	var coreState: CoreState = convert_mavlink_json_to_core_state(mavlinkJsonPacket)
	vehicle_core_state_received.emit(coreState)
