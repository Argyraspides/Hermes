using System;

namespace Hermes.Core.Vehicle;

public abstract class Component
{
    private ComponentType m_ComponentType = ComponentType.NULL;

    public ComponentType ComponentType
    {
        get { return m_ComponentType; }
        private set
        {
            if (m_ComponentType != ComponentType.NULL)
            {
                throw new ArgumentException(
                    "A component type cannot change after it has been set. Have you ever seen a GPS module turn into a rocket engine?");
            }

            m_ComponentType = value;
        }
    }

    public bool Enabled { get; private set; }
}
