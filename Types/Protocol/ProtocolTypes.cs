// Defines the various types of communication protocols that exist
// A communication protocol in the case of Hermes is any protocol
// that standardizes communication between Hermes and a UxV.
// For example, PX4 and ArduPilot drones use MAVLink (https://mavlink.io/en/)
// as their standard communication protocol.
public enum ProtocolType
{
    MAVLINKV1,
    MAVLINKV2,
    STANAG,
    VECTOR600,
    UNKNOWN
}