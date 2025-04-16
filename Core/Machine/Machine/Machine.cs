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

using System;
using Hermes.Common.HermesUtils;
using Hermes.Core.Machine.Capabilities;
using Hermes.Core.StateManagers;

namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Common.Map.Utils;



public partial class Machine : RigidBody3D, Selectable3D
{
    public MachineType MachineType { get; private set; } = MachineType.Unknown;
    public uint? MachineId { get; private set; }

    private Dictionary<uint, HellenicMessage> m_hellenicMessages = new Dictionary<uint, HellenicMessage>();
    private HashSet<Capability> m_capabilities = new HashSet<Capability>();
    private MeshInstance3D m_mesh;

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
        m_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
    }

    public void OnMouseEntered()
    {
        Console.WriteLine("OnMouseEntered");
        StandardMaterial3D mat = (StandardMaterial3D)m_mesh.Mesh.SurfaceGetMaterial(0);
        mat.AlbedoColor = new Color(0.0f, 0.0f, 1.0f);
    }

    public void OnMouseExited()
    {
        Console.WriteLine("OnMouseExited");
        StandardMaterial3D mat = (StandardMaterial3D)m_mesh.Mesh.SurfaceGetMaterial(0);
        mat.AlbedoColor = new Color(1.0f, 0.0f, 0.0f);
    }

    public void OnMouseClicked(MouseButton button)
    {
        Console.WriteLine("OnMouseClicked");
    }
}
