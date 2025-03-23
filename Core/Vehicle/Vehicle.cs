using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle.Components;
using Hermes.Core.Vehicle.Components.ComponentStates;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Core.Vehicle;

public partial class Vehicle : RigidBody3D
{
    private Dictionary<ComponentType, Component> m_components = new Dictionary<ComponentType, Component>();

    public override void _Ready()
    {
    }

    public void AddComponent(Component component)
    {
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

    // TODO::ARGYRASPIDES() { Not sure I like this ... A vehicle shouldn't be aware of the hellenic messaging system.
    // think about it later ... }
    public void UpdateComponent(HellenicMessage message)
    {
        foreach (Component component in m_components.Values)
        {
            component.UpdateComponentState(message);
        }
    }
}
