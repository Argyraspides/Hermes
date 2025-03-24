namespace Hermes.Core.Vehicle.Components;

using System;
using Hermes.Core.Vehicle.Components.ComponentStates;

public abstract class Component
{
    private ComponentType m_ComponentType = ComponentType.NULL;
    private ComponentState m_ComponentState;

    public ComponentType ComponentType
    {
        get { return m_ComponentType; }
        protected set
        {
            if (m_ComponentType != ComponentType.NULL)
            {
                throw new ArgumentException(
                    "A component type cannot change after it has been set. Have you ever seen a GPS module turn into a rocket engine?");
            }

            m_ComponentType = value;
        }
    }

    public abstract void UpdateComponentState(HellenicMessage message);
    public bool Enabled { get; protected set; } = true;
}
