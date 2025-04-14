/// <summary>
/// The compass heading of the object. 
/// </summary>
partial class Heading : HellenicMessage
{

	/// <summary>
	/// Heading in degrees [0..360) 
	/// </summary>
	public double? Hdg { get; set; }


	/// <summary>
	/// Reference frame (e.g., Magnetic North, True North - define via enum) 
	/// </summary>
	public byte? ReferenceFrame { get; set; }


	/// <summary>
	/// Timestamp (microseconds epoch or system boot) 
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public Heading()
	{
		Id = 3;
	}


	public Heading(
		uint pMachineId,
		uint pOriginalProtocol,
		double pHdg,
		byte pReferenceFrame,
		ulong pTimeUsec	)
	{
		Id = 3;
		Hdg = pHdg;
		ReferenceFrame = pReferenceFrame;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}
