using Hermes.Core.Vehicle.Components;

namespace Hermes.Core.Vehicle.States;

/// <summary>
/// Represents the position state of a vehicle, independent of which component provides the data.
/// Includes location coordinates, altitude, and reference frame information.
/// </summary>
public class PositionState
{
    /// <summary>
    /// Latitude (WGS84 or planetary model)
    /// </summary>
    public double Latitude { get; set; } = double.NaN;

    /// <summary>
    /// Longitude (WGS84 or planetary model)
    /// </summary>
    public double Longitude { get; set; } = double.NaN;

    /// <summary>
    /// Altitude (Mean Sea Level or reference frame)
    /// </summary>
    public double Altitude { get; set; } = double.NaN;

    /// <summary>
    /// Altitude relative to home/base
    /// </summary>
    public double RelativeAltitude { get; set; } = double.NaN;

    /// <summary>
    /// Reference frame (0 = Mercury, 1 = Venus, 2 = Earth, 3 = Moon, 4 = Mars...)
    /// Earth (2) by default
    /// </summary>
    public byte ReferenceFrame { get; set; } = 2;

    /// <summary>
    /// Timestamp of the most recent position update (microseconds)
    /// </summary>
    public ulong TimeUsec { get; set; } = 0;

    /// <summary>
    /// Optional tracking of which component provided each piece of data
    /// </summary>
    public ComponentType PositionSource { get; set; } = ComponentType.NULL;
    public ComponentType AltitudeSource { get; set; } = ComponentType.NULL;

    public PositionState()
    {
        // Defaults already set in property declarations
    }
}
