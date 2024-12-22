extends Node


# >>>>>>>>>>>>>>>> PY >>>>>>>>>>>>>>>>
# Python script which uses MAVSDK to listen in and parse MAVLink messages
var pythonLocalScriptPath: 		String 	= "res://Common/Communications/Protocols/MAVLink/pymavsdk.py"
var pythonAbsoluteScriptPath: 	String 	= ProjectSettings.globalize_path(pythonLocalScriptPath)

var pythonCommand: 			String 	= "python3"
var pythonThread: 			Thread
var pythonProcessId:		int		= -1

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pythonThread = Thread.new()
	pythonThread.start(run_python_mavlink_listener_script)

	
func _on_tree_exiting() -> void:
	kill_python_mavlink_listener_script()

# >>>>>>>>>>>>>>>> RUNNERS >>>>>>>>>>>>>>>>
func run_python_mavlink_listener_script() -> void:
	pythonProcessId = OS.create_process(pythonCommand, [pythonAbsoluteScriptPath])
	

# >>>>>>>>>>>>>>>> KILLERS >>>>>>>>>>>>>>>>
func kill_python_mavlink_listener_script() -> void:
	if pythonProcessId > 0: OS.kill(pythonProcessId)
