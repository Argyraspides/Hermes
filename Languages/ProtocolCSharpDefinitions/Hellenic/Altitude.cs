/// <summary>
/// The altitude of the object with high precision, applicable to both 
///  planetary  and deep-space operations.   
/// </summary>
partial class Altitude : HellenicMessage
{

	/// <summary>
	/// Altitude (Mean Sea Level or reference frame) 
	/// </summary>
	public double? Alt { get; set; }


	/// <summary>
	/// Altitude relative to home/base 
	/// </summary>
	public double? RelativeAlt { get; set; }


	/// <summary>
	/// Timestamp (since system boot) 
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
