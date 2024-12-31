using Godot;
using System;
using System.Diagnostics;
using System.Threading;

// The ExternalLauncher class' purpose is to launch any files/scripts external to Hermes.
// For example, at the moment, to avoid the need to write a custom MAVLink parser (which 
// would take forever especially given the firmware differences of MAVLink messages between, say
// PX4 and ArduPilot), Hermes uses a Python script with MAVSDK to listen in on and deserialize
// MAVLink messages before sending them over a WebSocket to Hermes. This ExternalLauncher
// is what invokes the Python script in the first place.
public partial class ExternalLauncher : Node
{

	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> PYTHON INVOKERS <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `

	private const string pythonMAVLinkListenerLocalScriptPath = "res://Common/Communications/Protocols/MAVLink/pymavsdk.py";
	private string pythonMAVLinkListenerAbsoluteScriptPath;

	// TODO: This may cause issues. Some systems use "python" while others use "python3"
	private string pythonCommand = "python3";

	private Thread pythonMAVLinkListenerThread;
	private Process pythonMAVLinkListenerProcess;
	private bool isPythonMAVLinkListenerProcessRunning = false;


	private void InitializePythonMAVLinkListener()
	{
		pythonMAVLinkListenerAbsoluteScriptPath = ProjectSettings.GlobalizePath(pythonMAVLinkListenerLocalScriptPath);
		pythonMAVLinkListenerThread = new Thread(RunPythonMavlinkListenerScript);
		pythonMAVLinkListenerThread.Start();
	}

	private void RunPythonMavlinkListenerScript()
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = pythonCommand,
				Arguments = pythonMAVLinkListenerAbsoluteScriptPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			pythonMAVLinkListenerProcess = new Process
			{
				StartInfo = startInfo,
				EnableRaisingEvents = true
			};

			// Setup event handlers for output and error streams
			pythonMAVLinkListenerProcess.OutputDataReceived += (sender, e) =>
			{
				if (e.Data != null)
				{
					GD.Print($"Python Output: {e.Data}");
				}
			};

			pythonMAVLinkListenerProcess.ErrorDataReceived += (sender, e) =>
			{
				if (e.Data != null)
				{
					GD.PrintErr($"Python Error: {e.Data}");
				}
			};

			pythonMAVLinkListenerProcess.Start();
			isPythonMAVLinkListenerProcessRunning = true;

			// Begin asynchronous read operations
			pythonMAVLinkListenerProcess.BeginOutputReadLine();
			pythonMAVLinkListenerProcess.BeginErrorReadLine();

			// Wait for the process to exit
			pythonMAVLinkListenerProcess.WaitForExit();
			isPythonMAVLinkListenerProcessRunning = false;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error running Python script: {e.Message}");
			isPythonMAVLinkListenerProcessRunning = false;
		}
	}

	private void KillPythonMavlinkListenerScript()
	{
		if (pythonMAVLinkListenerProcess != null && !pythonMAVLinkListenerProcess.HasExited)
		{
			try
			{
				pythonMAVLinkListenerProcess.Kill(true); 		// true for entire process tree
				pythonMAVLinkListenerProcess.WaitForExit(1000); 
				pythonMAVLinkListenerProcess.Dispose();
				isPythonMAVLinkListenerProcessRunning = false;
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error killing Python process: {e.Message}");
			}
		}

		if (pythonMAVLinkListenerThread != null && pythonMAVLinkListenerThread.IsAlive)
		{
			try
			{
				pythonMAVLinkListenerThread.Join(1000);
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error stopping Python thread: {e.Message}");
			}
		}
	}


	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
	//	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< PYTHON INVOKERS >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
	//	>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> GODOT <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
	//	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
	//	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
	//	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `

	// TODO: There are some cleanup functions in here which handle various scenarios
	// when Hermes quits (either an engine crash, normal close, node removal from scene, etc).
	// and kills any invoked Python scripts. For some reason this doesn't work when running
	// in debug mode and killing the process through VSCode. Not sure why. 
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializePythonMAVLinkListener();

		// _ExitTree() may not be called if the application crashes.
		// Here we make sure to kill the Python scripts even if it does crash.
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

		// If, for some reason, the user decides to launch Hermes via the terminal
		Console.CancelKeyPress += (sender, args) =>
		{
			args.Cancel = true; // Prevent immediate termination so we can perform cleanup
			KillPythonMavlinkListenerScript();
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnProcessExit(object sender, EventArgs e)
    {
        KillPythonMavlinkListenerScript();
    }

	// Called when the node is about to leave the SceneTree
    public override void _ExitTree()
    {
		KillPythonMavlinkListenerScript();
    }

	// Called when the object receives a notification, which can be identified in what by comparing it with a constant
    public override void _Notification(int what)
    {
		// _ExitTree() may not be called if the application crashes.
		// Here we make sure to kill the Python scripts even if it does crash.
        if(what == MainLoop.NotificationCrash)
		{
			KillPythonMavlinkListenerScript();
		}
    }

    //	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
    //	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
    //	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
    //	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< GODOT >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>


}
