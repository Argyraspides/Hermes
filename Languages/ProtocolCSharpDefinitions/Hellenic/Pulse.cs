	/// <summary>
	/// System status message indicating the entity is active and functioning.
	/// Used for health monitoring and presence detection.
	/// </summary>
partial class Pulse : HellenicMessage
{
	/// <summary>
	/// Type of machine
	/// </summary>
	public ushort? MachineType { get; set; }

	/// <summary>
	/// Machine identifier or callsign
	/// </summary>
	public string? Callsign { get; set; }

	/// <summary>
	/// Timestamp (microseconds since system boot)
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public Pulse()
	{
		Id = 4;
		MessageName = nameof(Pulse);
	}

	public Pulse(ushort pMachineType, string pCallsign, ulong pTimeUsec, uint pMachineId, uint pOriginalProtocol)
	{
		Id = 4;
		MessageName = nameof(Pulse);
		MachineType = pMachineType;
		Callsign = pCallsign;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}