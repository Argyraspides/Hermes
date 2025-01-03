extends Camera3D

@export var target_node: 			Node3D  # The sphere to orbit around
@export var distance: 				float = 6378.1370 * 2  # Starting distance from target
@export var min_distance: 			float = 6378.1370
@export var max_distance: 			float = 6378.1370 * 5
@export var rotation_speed: 		float = 0.005
@export var rotation_speed_divider: float = 15
@export var zoom_speed: 			float = 25
@export var zoom_speed_divider:		float = 100

@onready var omni_light: OmniLight3D = $OmniLight3D

var latitude: 		float = 0.0  	# Up/down rotation
var longitude: 		float = 0.0  	# Left/right rotation
var min_latitude: 	float = -80.0  	# Prevent camera from going under/over the sphere
var max_latitude: 	float = 80.0

func _ready():
	# Initialize camera position
	update_camera_position()

func _unhandled_input(event):
	zoom_speed = distance / zoom_speed_divider
	rotation_speed = 1 / (distance / rotation_speed_divider)
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distance = max(min_distance, distance - zoom_speed)
			update_camera_position()
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distance = min(max_distance, distance + zoom_speed)
			update_camera_position()
		
	elif event is InputEventMouseMotion and event.button_mask == MOUSE_BUTTON_LEFT:
		# Rotate camera when left mouse button is held
		longitude -= event.relative.x * rotation_speed
		latitude = clamp(latitude + event.relative.y * rotation_speed, 
						deg_to_rad(min_latitude), 
						deg_to_rad(max_latitude))
		update_camera_position()

func update_camera_position():
	# Convert spherical coordinates to Cartesian coordinates
	var pos = Vector3()
	var lat_cos = cos(latitude)
	
	# Calculate new position
	pos.x = distance * lat_cos * sin(longitude)
	pos.y = distance * sin(latitude)
	pos.z = distance * lat_cos * cos(longitude)
	
	# Update camera transform
	global_transform.origin = target_node.global_transform.origin + pos
	look_at(target_node.global_transform.origin)
	
	#omni_light.global_position.z = distance
	
