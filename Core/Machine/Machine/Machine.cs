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

using Hermes.Core.Machine.Capabilities;

namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Common.Map.Utils;



public partial class Machine : RigidBody3D
{
    public MachineType MachineType { get; private set; } = MachineType.Unknown;
    public uint? MachineId { get; private set; }

    private Dictionary<uint, HellenicMessage> m_hellenicMessages = new Dictionary<uint, HellenicMessage>();
    private HashSet<Capability> m_capabilities = new HashSet<Capability>();

    // Last time this vehicle was updated in the Unix timestamp
    public double LastUpdateTimeUnix { get; private set; } = 0;

    public void Update(HellenicMessage message)
    {
        UpdateMessages(message);
        UpdateIdentity(message);
        UpdatePosition(message);
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
        CollisionLayer = 1;
    }

    // TODO::ARGYRASPIDES() { Figure out why this doesn't get called, but "_UnhandledInput" does ... even though
    // the documentation for this _InputEvent function says "Receives unhandled InputEvents." ??????
    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        base._InputEvent(camera, @event, eventPosition, normal, shapeIdx);

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            if (mouseButtonEvent.IsPressed() && mouseButtonEvent.ButtonIndex == MouseButton.Left)
            {
                // Perform a raycast to check if the click intersected with this vehicle's collision shape.
                PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
                Vector3 from = GetViewport().GetCamera3D().ProjectRayOrigin(mouseButtonEvent.Position);
                Vector3 to = from + GetViewport().GetCamera3D().ProjectRayNormal(mouseButtonEvent.Position) * 1000; // Adjust the length as needed

                PhysicsRayQueryParameters3D queryParams = new PhysicsRayQueryParameters3D
                {
                    From = from,
                    To = to,
                    CollideWithBodies = true,
                    Exclude = new Godot.Collections.Array<Rid> { GetRid() } // Ignore this vehicle's own RID
                };

                Godot.Collections.Dictionary result = spaceState.IntersectRay(queryParams);

                if (result.ContainsKey("collider") && (Node3D)result["collider"] == this)
                {
                    // The click hit this vehicle!
                    GD.Print("Vehicle clicked!");
                    // Add your desired logic here (e.g., select the vehicle, open a UI, etc.)
                }
            }
        }
    }
}
