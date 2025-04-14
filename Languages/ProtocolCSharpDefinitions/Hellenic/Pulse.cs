/// <summary>
///   System status message indicating the entity is active and 
/// provides basic identification.  Used for health monitoring and presence detection. Akin 
/// to a Heartbeat.   
/// </summary>
partial class Pulse : HellenicMessage
{

	/// <summary>
	/// Type of machine 
	/// </summary>
	public ushort? MachineType { get; set; }


	/// <summary>
	/// Machine identifier or callsign (if available) 
	/// </summary>
	public string? Callsign { get; set; }


	/// <summary>
	/// Timestamp (microseconds epoch or system boot) 
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public Pulse()
	{
		Id = 4;
	}


	public Pulse(
		uint pMachineId,
		uint pOriginalProtocol,
		ushort pMachineType,
		string pCallsign,
		ulong pTimeUsec	)
	{
		Id = 4;
		MachineType = pMachineType;
		Callsign = pCallsign;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}
