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


public partial class Vehicle : RigidBody3D
{

    public MachineType MachineType { get; private set; } = MachineType.Unknown;

    private Dictionary<ComponentType, Component> m_components = new Dictionary<ComponentType, Component>();

    public override void _Ready()
    {
    }

    public void AddComponent(Component component)
    {
        if(component == null || component.ComponentType == ComponentType.NULL)
        {
            // throw new ArgumentNullException("Cannot add a null component to vehicle");
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
        return m_components[componentType];
    }

    private void UpdateComponent(HellenicMessage message)
    {
        foreach (Component component in m_components.Values)
        {
            component.UpdateComponentState(message);
        }
    }

    // TODO::ARGYRASPIDES() { Not sure I like this ... A vehicle shouldn't be aware of the hellenic messaging system.
    // think about it later ... }
    private void UpdateIDProperties(HellenicMessage message)
    {
        if (message.Id == (uint)HellenicMessageType.Pulse)
        {
            Pulse pulseMessage = (Pulse)message;
            this.MachineType = (MachineType)pulseMessage.VehicleType;
        }
    }

    public void Update(HellenicMessage message)
    {
        UpdateComponent(message);
        UpdateIDProperties(message);
    }
}
