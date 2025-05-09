/// <summary>
/// The altitude of the object. Part of the core position data. 
/// </summary>
partial class Altitude : HellenicMessage
{

	/// <summary>
	/// Altitude (Mean Sea Level or reference frame origin) 
	/// </summary>
	public double? Alt { get; set; }


	/// <summary>
	/// Altitude relative to home/ground/takeoff location 
	/// </summary>
	public double? RelativeAlt { get; set; }


	/// <summary>
	/// Timestamp (microseconds epoch or system boot) 
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public Altitude()
	{
		Id = 1;
	}


	public Altitude(
		uint pMachineId,
		uint pOriginalProtocol,
		double pAlt,
		double pRelativeAlt,
		ulong pTimeUsec	)
	{
		Id = 1;
		Alt = pAlt;
		RelativeAlt = pRelativeAlt;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}
