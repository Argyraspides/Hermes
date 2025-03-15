namespace Hermes.Core.Vehicle;

public abstract class Component
{
    ComponentType m_ComponentType;
    private bool m_IsEnabled;

    public void enable()
    {
        m_IsEnabled = true;
    }

    public void disable()
    {
        m_IsEnabled = false;
    }

    public bool IsEnabled()
    {
        return m_IsEnabled;
    }
}
