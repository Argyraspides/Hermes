namespace Hermes.Core.Machine.Capabilities;

public abstract class Capability
{
    // ID of the machine that this capability will invoke for
    private uint m_machineId;
    public abstract void InvokeCapability();

    // Okay heres what I htink:
    /*
     * There will be one function that will just be InvokeCapability which is above. This will somehow have to invoke a function
     * in a command translation layer for MAVLink that will call like idk "takeoff". This translation layer will eventually have
     * to reply back to the capability either directly or indirectly.
     *
     * So InvokeCapability should:
     * - Indirectly or directly call the mavlink command interface or some shit
     * - The command niterface will forward the command via mavlink. The mavlink state machine will handle it
     * - The interface or something else will reply to our capability. Our capability should be awaiting in the meantime or smth
     * - and there will be retries/timeouts or whatever.
     */
}
