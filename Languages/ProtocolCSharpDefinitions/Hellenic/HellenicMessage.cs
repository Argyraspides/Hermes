#nullable enable

using Godot;


public abstract partial class HellenicMessage : RefCounted
{
	// The ID of the machine this message was sent from
	public uint? MachineId { get; protected set; }
	// The ID of the Hellenic message itself. E.g., An ID of 0 corresponds to "LatitudeLongitude"
	public uint? Id { get; protected set; }
	// The name of this Hellenic message, e.g., "LatitudeLongitude"
	public string? MessageName { get; protected set; }
	// The original protocol from which this Hellenic message was created
	public uint? OriginalProtocol { get; protected set; }
}