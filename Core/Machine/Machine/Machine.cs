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


using Hermes.Core.Machine.Machine.Capabilities;

namespace Hermes.Core.Machine.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Common.Map.Utils;
using System;
using Hermes.Common.HermesUtils;
using Core.Machine.Machine.Capabilities;
using Hermes.Core.StateManagers;
using Hermes.Universe.Autoloads.EventBus;


public partial class Machine : RigidBody3D, Selectable3D
{
    public MachineType MachineType { get; private set; } = MachineType.Unknown;
    public uint? MachineId { get; private set; }

    // TODO::ARGYRASPIDES() { "Selected" should not be in the vehicle class. Should make like a selection class with its own
    // capabilities for how things should be selected and do stuff there. Here now for testing }
    public bool Selected { get; set; } = false;

    private Dictionary<uint, HellenicMessage> m_hellenicMessages = new Dictionary<uint, HellenicMessage>();
    private HashSet<Capability> m_capabilities = new HashSet<Capability>();

    // Last time this vehicle was updated in the Unix timestamp
    public double LastUpdateTimeUnix { get; private set; } = 0;

    public void Update(HellenicMessage message)
    {
        UpdateMessages(message);
        UpdateIdentity(message);
        UpdatePosition(message);
        //StandardMaterial3D mat = (StandardMaterial3D)m_mesh.Mesh.SurfaceGetMaterial(0);
        //mat.AlbedoColor = new Color(1.0f, 0.0f, 0.0f);
    }

    private void UpdateMessages(HellenicMessage message)
    {
        if (message == null || !message.Id.HasValue) return;

        LastUpdateTimeUnix = Time.GetUnixTimeFromSystem();
        m_hellenicMessages[message.Id.Value] = message;
    }

    private void UpdateIdentity(HellenicMessage message)
    {
        if (message is Pulse pulse)
        {
            MachineType =
                pulse.MachineType.HasValue ?
                    (MachineType)pulse.MachineType.Value : MachineType.Unknown;
            MachineId = pulse.MachineId;
        }
    }

    private void UpdatePosition(HellenicMessage message)
    {
        if (message == null || !message.Id.HasValue) return;
        if (message is not LatitudeLongitude location) return;

        if (location.Lat.HasValue && location.Lon.HasValue)
        {
            GlobalPosition = MapUtils.LatLonToCartesian(
                Mathf.DegToRad((float)location.Lat),
                Mathf.DegToRad((float)location.Lon),
                (ReferenceFrame)location.ReferenceFrame);
        }
    }

    public HellenicMessage GetHellenicMessage(HellenicMessageType messageType)
    {
        return m_hellenicMessages.TryGetValue((uint)messageType, out var hellenicMessage) ? hellenicMessage : null;
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
        Console.WriteLine("OnMouseClicked");
    }
}
