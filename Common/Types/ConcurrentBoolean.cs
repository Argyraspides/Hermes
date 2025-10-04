namespace Hermes.Common.Types;

public class ConcurrentBoolean
{
    private bool m_value;
    private readonly object m_lock = new object();

    public ConcurrentBoolean(bool initialValue)
    {
        m_value = initialValue;
    }

    public bool Value
    {
        get
        {
            lock (m_lock)
            {
                return m_value;
            }
        }
        set
        {
            lock (m_lock)
            {
                m_value = value;
            }
        }
    }

    public void Set(bool newVal)
    {
        lock (m_lock)
        {
            m_value = newVal;
        }
    }

    public bool Toggle()
    {
        lock (m_lock)
        {
            m_value = !m_value;
            return m_value;
        }
    }
}
