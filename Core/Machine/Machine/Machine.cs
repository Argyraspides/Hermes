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


using Hermes.Core.Machine.CapabilityEngine;

namespace Hermes.Core.Machine.Machine;

using Godot;
using System.Collections.Generic;
using System;
using Hermes.Core.StateManagers;
using Hermes.Core.Autoloads.EventBus;


public partial class Machine : RigidBody3D, Selectable3D
{
    public MachineType MachineType { get; set; } = MachineType.Unknown;
    public uint? MachineId { get; set; }

    // TODO::ARGYRASPIDES() { "Selected" should not be in the vehicle class. Should make like a selection class with its own
    // capabilities for how things should be selected and do stuff there. Here now for testing }
    public bool Selected { get; set; } = false;

    public Dictionary<uint, HellenicMessage> HellenicMessages { get; set; } = new Dictionary<uint, HellenicMessage>();
    public HashSet<Capability> Capabilities { get; set; } = new HashSet<Capability>();

    // Last time this vehicle was updated in the Unix timestamp
    public double LastUpdateTimeUnix { get; set; } = 0;

    public HellenicMessage GetHellenicMessage(HellenicMessageType messageType)
    {
        return HellenicMessages.TryGetValue((uint)messageType, out var hellenicMessage) ? hellenicMessage : null;
    }

    public IEnumerable<Capability> GetCapabilities()
    {
        return Capabilities;
    }

    public override void _Ready()
    {
        InputRayPickable = true;
        CollisionLayer = HermesSettings.SELECTABLE_LAYER;
    }

    public void OnMouseEntered()
    {
        Console.WriteLine("OnMouseEntered");
    }

    public void OnMouseExited()
    {
        Console.WriteLine("OnMouseExited");
    }

    public void OnMouseClicked(MouseButton button)
    {
        GlobalEventBus.Instance.UIEventBus.OnMachineClicked(this);
    }
}
