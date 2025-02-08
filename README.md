# Hermes: Messenger of the Machines
<p align="center">
  <img src="https://github.com/user-attachments/assets/96d8bcdb-5f00-4acb-9837-756f1ef2c2fc" width="300" height="300">
</p>

Hermes is a ground control station software that aims to be:
- Protocol agnostic
- Vehicle agnostic
- Highly flexible and extensible
- Video-game like
- 6.022 * 10^23 times better than the decrepit QGroundControl that has the worst spaghetti code backend known to mankind


## Setup Instructions

### Step 1: Downlaod the Godot Game Engine
Ensure you have [Godot 4.3 Mono installed](https://godotengine.org/). It is important you download the mono version (.NET) and not the regular version as Hermes is written entirely in C#.

You can get up and running with the Godot editor itself and nothing more, though it is recommended to use VSCode/JetBrains Rider as the built-in Godot editor has poor support for C#, meanwhile VSCode and JetBrains Rider both have good support for Godot development in GDScript and C#.

### Step 2 (Optional): VSCode Setup
Download the C# Dev Kit VSCode extension from Microsoft, and the godot-tools extension by Geequlim
<br></br>
![image](https://github.com/user-attachments/assets/42460577-6807-4578-9d22-c7c5ae28c316)
<br></br>
![image](https://github.com/user-attachments/assets/aac338d4-8b89-4afc-a5e8-7f63abb763b8)

Under your ``.vscode`` folder, paste the following into your ``launch.json`` file, making sure to edit the path to your Godot executable in:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (console)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "/path/to/your/godot/executable",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "console": "internalConsole"
        },
        {
            "name": ".NET Core Attach",
            "type": "coreclr",
            "request": "attach",
        }
    ]
}
```

Under your ``.vscode`` folder, paste the following into your ``settings.json`` file making sure to edit the path to your Godot executable in:

```json
{
    "godotTools.editorPath.godot4": "/path/to/your/godot/executable"
}
```

Under your ``.vscode`` folder, paste the following into your ``tasks.json`` file:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
        }
    ]
}
```

You should now be able to run & debug Hermes.


### Step 3 (Optional -- Reccommended): JetBrains Rider Setup
Simply download [JetBrains Rider](https://www.jetbrains.com/rider/download) and open up the Godot cloned repo. That's it. JetBrains has a [baked-in plugin for Godot development](https://www.jetbrains.com/help/rider/Godot.html#running-and-debugging) which should get you up and running right out of the box with debugging support.
