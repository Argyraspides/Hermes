using Hermes.Core.Vehicle.Components;

namespace Hermes.Core.Vehicle.States;

/// <summary>
/// Represents the velocity state of a vehicle, independent of which component provides the data.
/// Includes ground velocity components in all three axes.
/// </summary>
public class VelocityState
{
    /// <summary>
    /// Velocity X (Latitude direction, positive north)
    /// </summary>
    public double VelocityX { get; set; } = double.NaN;

    /// <summary>
    /// Velocity Y (Longitude direction, positive east)
    /// </summary>
    public double VelocityY { get; set; } = double.NaN;

    /// <summary>
    /// Velocity Z (Altitude direction, positive up)
    /// </summary>
    public double VelocityZ { get; set; } = double.NaN;

    /// <summary>
    /// Total ground speed (calculated from X, Y components)
    /// </summary>
    public double GroundSpeed
    {
        get
        {
            if (double.IsNaN(VelocityX) || double.IsNaN(VelocityY))
                return double.NaN;

            return System.Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
        }
    }

    /// <summary>
    /// Timestamp of the most recent velocity update (microseconds)
    /// </summary>
    public ulong TimeUsec { get; set; } = 0;

    /// <summary>
    /// Optional tracking of which component provided velocity data
    /// </summary>
    public ComponentType VelocitySource { get; set; } = ComponentType.NULL;

    public VelocityState()
    {
        // Defaults already set in property declarations
    }
}
