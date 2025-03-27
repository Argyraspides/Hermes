namespace Hermes.Core.Machine.States;

/// <summary>
/// Represents the identity information of a machine, independent of which component provides the data.
/// Includes machine type, callsign, and other identifying information.
/// </summary>
public class IdentityState
{

    /// <summary>
    /// Unique ID of the machine
    /// </summary>
    public uint MachineId { get; set; }

    /// <summary>
    /// Type of machine
    /// </summary>
    public MachineType MachineType { get; set; } = MachineType.Unknown;

    /// <summary>
    /// Machine identifier or callsign
    /// </summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the most recent identity update (microseconds)
    /// </summary>
    public ulong TimeUsec { get; set; } = 0;

    public IdentityState()
    {
    }
}
