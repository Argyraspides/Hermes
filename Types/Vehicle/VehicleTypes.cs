using Godot;

// Defines the various types of vehicles that can be represented in the system
public enum VehicleType
{
    // Generic Types
    GenericMulticopter,
    GenericQuadcopter,
    GenericHexacopter,
    GenericOctocopter,
    GenericFixedWing,
    GenericRover,
    GenericSubmarine,
    GenericSurfaceVessel,

    // Special Types
    F35CLightning,
    Sr71Blackbird,
    SaturnV,

    // Unknown Types
    Unknown
}