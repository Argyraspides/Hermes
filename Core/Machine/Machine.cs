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
using Godot.Collections;
using Hermes.Common.Map.Utils;

namespace Hermes.Core.Machine;


using Godot;
using System.Collections.Generic;

public partial class Machine : RigidBody3D
{
    public MachineType MachineType { get; private set; } = MachineType.Unknown;
    public uint? MachineId { get; private set; }

    private Dictionary<uint, HellenicMessage> m_hellenicMessages = new Dictionary<uint, HellenicMessage>();

    // Last time this vehicle was updated in the Unix timestamp
    public double LastUpdateTimeUnix { get; private set; } = 0;

    public void Update(HellenicMessage message)
    {
        UpdateState(message);
        UpdateIdentity(message);
    }

    private void UpdateState(HellenicMessage message)
    {
        if (!message.Id.HasValue) return;

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

    public HellenicMessage GetHellenicMessage(HellenicMessageType messageType)
    {
        return m_hellenicMessages.TryGetValue((uint)messageType, out var hellenicMessage) ? hellenicMessage : null;
    }
}
