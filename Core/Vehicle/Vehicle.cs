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


namespace Hermes.Core.Vehicle;

using System;
using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle.Components;
using Hermes.Core.Vehicle.States;

public partial class Vehicle : RigidBody3D
{

    // ********* States that all vehicles regardless of type should have
    public PositionState Position { get; } = new PositionState();
    public OrientationState Orientation { get; } = new OrientationState();
    public VelocityState Velocity { get; } = new VelocityState();
    public IdentityState Identity { get; } = new IdentityState();

    private Dictionary<ComponentType, Component> m_components = new Dictionary<ComponentType, Component>();

    // Last time this vehicle was updated in the Unix timestamp
    public double LastUpdateTimeUnix { get; private set; } = 0;

    public override void _Ready()
    {
    }

    public void AddComponent(Component component)
    {
        if(component == null || component.ComponentType == ComponentType.NULL)
        {
            return;
        }
        m_components.TryAdd(component.ComponentType, component);
    }

    public bool HasComponent(ComponentType componentType)
    {
        return m_components.ContainsKey(componentType);
    }

    public Component GetComponent(ComponentType componentType)
    {
        if (!m_components.ContainsKey(componentType))
        {
            return null;
        }
        return m_components[componentType];
    }

    private void UpdateComponents(HellenicMessage message)
    {
        foreach (Component component in m_components.Values)
        {
            component.UpdateComponentState(message);
        }
    }

    public void Update(HellenicMessage message)
    {
        HellenicStateUpdater.UpdateStates(message, Position, Orientation, Velocity, Identity);
        UpdateComponents(message);
        LastUpdateTimeUnix = Time.GetUnixTimeFromSystem();
    }

    public MachineType MachineType
    {
        get { return Identity.VehicleType; }
    }
}
