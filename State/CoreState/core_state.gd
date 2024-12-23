## CoreState is a generic state object that applies to all vehicles regardless of their type.
## All vehicles, being physical entities of the universe, must have properties
## such as velocity, acceleration, position, etc.
## 
## Note: Any state variables with a numeric value are initialized to infinity, which means
## they have not been initialized/set. This is so when a core state is applied to a vehicle,
## any state not set can be ignored. This also allows CoreState to be a standalone module
extends Node
class_name CoreState


## Denotes the broad type of vehicle
var m_vehicleType	:= VehicleTypes.VehicleType.UNKNOWN

## Vehicle identifier. There is no guarantee
## that two vehicles that are different in real life
## are going to transmit different vehicle identifier values.
## The behavior of Hermes in such a case would be one in-game
## vehicle constantly switching states
var m_vehicleId		: int 		= INF


## X = LAT, degrees
## Y = LONG, degrees
## Z = ALT (from Mean Sea Level), millimeters 
var m_earthPosition	: Vector3 	= Vector3.INF

## Clockwise from north, degrees
var m_earthHeading	: float		= INF

## Represents distance away relative to home position on Earth's surface
## X = LAT dist, degrees
## Y = LONG dist, degrees
## Z = ALT dist (from home position), millimeters
var m_localPosition	: Vector3 	= Vector3.INF

## X = ROLL
## Y = PITCH
## Z = YAW
## Where: -pi < (X,Y,Z) < pi -> rad
var m_attitude		: Vector3 	= Vector3.INF

## X = LAT, +ve NORTH, m/s
## Y = LON, +ve EAST,  m/s
## Z = ALT, +ve DOWN,  m/s
var m_groundVel		: Vector3 	= Vector3.INF

## X = LAT, +ve NORTH
## Y = LON, +ve EAST
## Z = ALT, +ve DOWN
var m_groundAcc		: Vector3 	= Vector3.INF

func copy_known_fields(fromState: CoreState) -> void:
	if fromState == null:
		return
		
	if fromState.m_vehicleType != VehicleTypes.VehicleType.UNKNOWN:
		m_vehicleType = fromState.m_vehicleType
	
	if fromState.m_vehicleId 		!= INF:
		m_vehicleId = fromState.m_vehicleId
		
	if fromState.m_earthHeading 	!= INF:
		m_earthHeading = fromState.m_earthHeading
	
	if fromState.m_earthPosition 	!= Vector3.INF:
		m_earthPosition = fromState.m_earthPosition
		
	if fromState.m_localPosition 	!= Vector3.INF:
		m_localPosition = fromState.m_localPosition
		
	if fromState.m_attitude 		!= Vector3.INF:
		m_attitude = fromState.m_attitude
		
	if fromState.m_groundVel 		!= Vector3.INF:
		m_groundVel = fromState.m_groundVel
		
	if fromState.m_groundAcc 		!= Vector3.INF:
		m_groundAcc = fromState.m_groundAcc
