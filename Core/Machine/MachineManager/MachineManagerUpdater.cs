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


using Hermes.Common.HermesUtils;
namespace Hermes.Core.Machine;

using Godot;
using System.Collections.Generic;
using Hermes.Core.Machine.CapabilityEngine;
using Hermes.Core.Autoloads.EventBus;

// Partial class that only has updater functions to update a machine for all properties that it may contain
public partial class MachineManager : Node
{
    public void UpdateMachine(Machine.Machine machine, HellenicMessage message)
    {
        UpdateMessages(machine, message);
        UpdateIdentity(machine, message);
        UpdatePosition(machine, message);
    }

    private void UpdateMessages(Machine.Machine machine, HellenicMessage message)
    {
        if (message == null || !message.Id.HasValue) return;

        machine.LastUpdateTimeUnix = Time.GetUnixTimeFromSystem();
        machine.HellenicMessages[message.Id.Value] = message;
    }

    private void UpdateIdentity(Machine.Machine machine, HellenicMessage message)
    {
        if (message is Pulse pulse)
        {
            machine.MachineType =
                pulse.MachineType.HasValue ?
                    (MachineType)pulse.MachineType.Value : MachineType.Unknown;
            machine.MachineId = pulse.MachineId;
        }
    }

    private void UpdatePosition(Machine.Machine machine, HellenicMessage message)
    {
        if (message == null || !message.Id.HasValue) return;
        if (message is not LatitudeLongitude location) return;

        if (location.Lat.HasValue && location.Lon.HasValue)
        {
        }
    }

}
