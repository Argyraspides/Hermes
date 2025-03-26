namespace Hermes.Core.Vehicle.States;

/// <summary>
/// Represents the identity information of a vehicle, independent of which component provides the data.
/// Includes vehicle type, callsign, and other identifying information.
/// </summary>
public class IdentityState
{

    /// <summary>
    /// Unique ID of the vehicle
    /// </summary>
    public uint VehicleId { get; set; }

    /// <summary>
    /// Type of vehicle
    /// </summary>
    public MachineType VehicleType { get; set; } = MachineType.Unknown;

    /// <summary>
    /// Vehicle identifier or callsign
    /// </summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the most recent identity update (microseconds)
    /// </summary>
    public ulong TimeUsec { get; set; } = 0;

    public IdentityState()
    {
        // Defaults already set in property declarations
    }
}
