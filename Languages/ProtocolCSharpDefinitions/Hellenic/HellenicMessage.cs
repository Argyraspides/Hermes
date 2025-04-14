#nullable enable

using Godot;

public abstract partial class HellenicMessage : RefCounted
{

	/// <summary>
	/// ID of the machine that sent this message 
	/// </summary>
	public uint? MachineId { get; protected set; }


	/// <summary>
	/// The protocol from which this Hellenic message originated 
	/// </summary>
	public uint? OriginalProtocol { get; protected set; }


	/// <summary>
	/// The ID of the Hellenic message itself. E.g., An ID of 
	/// 0 corresponds to  "LatitudeLongitude"   
	/// </summary>
	public uint? Id { get; protected set; }

}
