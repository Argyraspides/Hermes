/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/

namespace Hermes.Common.Communications.ExternalLaunchers;

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

    private const string m_PYTHON_MAVLINK_LISTENER_LOCAL_SCRIPT_PATH =
        "res://Common/Communications/Protocols/MAVLink/MAVLinkInterface.py";

    private string m_pythonMAVLinkListenerAbsoluteScriptPath;

    private string m_pythonCommand = "python3";

    private Thread m_pythonMAVLinkListenerThread;
    private Process m_pythonMAVLinkListenerProcess;
    private bool m_isPythonMAVLinkListenerProcessRunning = false;

    // Some systems might use "python" as the CLI argument to invoke the Python interpreter, and others
    // may use "python3". In future, "python4" may be a thing. Here we detect how the user has
    // configured theirs so that we can run any dependent Python scripts gracefully
    private string DetectPythonCommand()
    {
        string[] pythonCommands = { "python3", "python" };
        foreach (string cmd in pythonCommands)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        GD.Print($"Detected Python command: {cmd}");
                        return cmd;
                    }
                }
            }
            catch (Exception e)
            {
                GD.Print($"Python detection error for {cmd}: {e.Message}");
            }
        }

        GD.PrintErr("No compatible Python interpreter found. Please install Python 3.");
        return null;
    }

    private void InitializePythonMAVLinkListener()
    {
        m_pythonMAVLinkListenerAbsoluteScriptPath =
            ProjectSettings.GlobalizePath(m_PYTHON_MAVLINK_LISTENER_LOCAL_SCRIPT_PATH);
        m_pythonMAVLinkListenerThread = new Thread(RunPythonMavlinkListenerScript);
        m_pythonMAVLinkListenerThread.Start();
    }

    private void RunPythonMavlinkListenerScript()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = m_pythonCommand,
                Arguments = m_pythonMAVLinkListenerAbsoluteScriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            m_pythonMAVLinkListenerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // Setup event handlers for output and error streams
            m_pythonMAVLinkListenerProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    GD.Print($"Python Output: {e.Data}");
                }
            };

            m_pythonMAVLinkListenerProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    GD.PrintErr($"Python Error: {e.Data}");
                }
            };

            m_pythonMAVLinkListenerProcess.Start();
            m_isPythonMAVLinkListenerProcessRunning = true;

            // Begin asynchronous read operations
            m_pythonMAVLinkListenerProcess.BeginOutputReadLine();
            m_pythonMAVLinkListenerProcess.BeginErrorReadLine();

            // Wait for the process to exit
            m_pythonMAVLinkListenerProcess.WaitForExit();
            m_isPythonMAVLinkListenerProcessRunning = false;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error running Python script: {e.Message}");
            m_isPythonMAVLinkListenerProcessRunning = false;
        }
    }

    private void KillPythonMavlinkListenerScript()
    {
        if (m_pythonMAVLinkListenerProcess != null && !m_pythonMAVLinkListenerProcess.HasExited)
        {
            try
            {
                m_pythonMAVLinkListenerProcess.Kill(true); // true for entire process tree
                m_pythonMAVLinkListenerProcess.WaitForExit(1000);
                m_pythonMAVLinkListenerProcess.Dispose();
                m_isPythonMAVLinkListenerProcessRunning = false;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error killing Python process: {e.Message}");
            }
        }

        if (m_pythonMAVLinkListenerThread != null && m_pythonMAVLinkListenerThread.IsAlive)
        {
            try
            {
                m_pythonMAVLinkListenerThread.Join(1000);
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
    // in debug mode and killing the process through VSCode (you have to close the game normally with the
    // 'x' button otherwise the invoked Python scripts will continue running). Not sure why.

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        m_pythonCommand = DetectPythonCommand();

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
        if (what == MainLoop.NotificationCrash)
        {
            KillPythonMavlinkListenerScript();
        }
    }

    //	  .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.
    //	:::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\
    //	'      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `
    //	<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< GODOT >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
}
