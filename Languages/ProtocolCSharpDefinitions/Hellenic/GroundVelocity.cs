	/// <summary>
	/// The velocity components of the object
	/// </summary>
partial class GroundVelocity : HellenicMessage
{
	/// <summary>
	/// Velocity X (Latitude direction, positive north)
	/// </summary>
	public double? Vx { get; set; }

	/// <summary>
	/// Velocity Y (Longitude direction, positive east)
	/// </summary>
	public double? Vy { get; set; }

	/// <summary>
	/// Velocity Z (Altitude direction, positive up)
	/// </summary>
	public double? Vz { get; set; }

	/// <summary>
	/// Timestamp (since system boot)
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public GroundVelocity()
	{
		Id = 2;
		MessageName = nameof(GroundVelocity);
	}

	public GroundVelocity(double pVx, double pVy, double pVz, ulong pTimeUsec, uint pMachineId, uint pOriginalProtocol)
	{
		Id = 2;
		MessageName = nameof(GroundVelocity);
		Vx = pVx;
		Vy = pVy;
		Vz = pVz;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}