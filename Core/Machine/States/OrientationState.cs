using Hermes.Core.Machine.Components;

namespace Hermes.Core.Machine.States;

/// <summary>
/// Represents the orientation state of a machine, independent of which component provides the data.
/// Includes heading, and future expansion for pitch, roll, etc.
/// </summary>
public class OrientationState
{
    /// <summary>
    /// Heading in degrees (0-360, 0 = North)
    /// </summary>
    public double Heading { get; set; } = double.NaN;

    /// <summary>
    /// Pitch angle in degrees (not yet implemented in messages)
    /// </summary>
    public double Pitch { get; set; } = double.NaN;

    /// <summary>
    /// Roll angle in degrees (not yet implemented in messages)
    /// </summary>
    public double Roll { get; set; } = double.NaN;

    /// <summary>
    /// Yaw angle in degrees (not yet implemented in messages)
    /// </summary>
    public double Yaw { get; set; } = double.NaN;

    /// <summary>
    /// Reference frame (0 = Mercury, 1 = Venus, 2 = Earth, 3 = Moon, 4 = Mars...)
    /// Earth (2) by default
    /// </summary>
    public byte ReferenceFrame { get; set; } = 2;

    /// <summary>
    /// Timestamp of the most recent orientation update (microseconds)
    /// </summary>
    public ulong TimeUsec { get; set; } = 0;

    /// <summary>
    /// Optional tracking of which component provided each piece of data
    /// </summary>
    public ComponentType HeadingSource { get; set; } = ComponentType.NULL;
    public ComponentType AttitudeSource { get; set; } = ComponentType.NULL;

    public OrientationState()
    {
    }
}
