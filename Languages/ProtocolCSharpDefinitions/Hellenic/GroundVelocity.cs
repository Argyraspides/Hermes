/// <summary>
/// The ground velocity components of the object in NED or ENU 
/// frame (consistent frame needed).   
/// </summary>
partial class GroundVelocity : HellenicMessage
{

	/// <summary>
	/// Velocity X (e.g., North or East depending on chosen frame) 
	/// </summary>
	public double? Vx { get; set; }


	/// <summary>
	/// Velocity Y (e.g., East or North depending on chosen frame) 
	/// </summary>
	public double? Vy { get; set; }


	/// <summary>
	/// Velocity Z (Down or Up depending on chosen frame - Hellenic 
	/// prefers positive Up) 
	/// </summary>
	public double? Vz { get; set; }


	/// <summary>
	/// Timestamp (microseconds epoch or system boot) 
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public GroundVelocity()
	{
		Id = 2;
	}


	public GroundVelocity(
		uint pMachineId,
		uint pOriginalProtocol,
		double pVx,
		double pVy,
		double pVz,
		ulong pTimeUsec	)
	{
		Id = 2;
		Vx = pVx;
		Vy = pVy;
		Vz = pVz;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}
