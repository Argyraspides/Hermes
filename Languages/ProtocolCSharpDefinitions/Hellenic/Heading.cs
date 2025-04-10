	/// <summary>
	/// The velocity components of the object
	/// </summary>
partial class Heading : HellenicMessage
{
	/// <summary>
	/// Heading in degrees
	/// </summary>
	public double? Hdg { get; set; }

	/// <summary>
	/// Reference frame (0 = Mercury, 1 = Venus, 2 = Earth, 3 = Moon, 4 = Mars ...)
	/// </summary>
	public byte? ReferenceFrame { get; set; }

	/// <summary>
	/// Timestamp (since system boot)
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public Heading()
	{
		Id = 3;
		MessageName = nameof(Heading);
	}

	public Heading(double pHdg, byte pReferenceFrame, ulong pTimeUsec, uint pMachineId, uint pOriginalProtocol)
	{
		Id = 3;
		MessageName = nameof(Heading);
		Hdg = pHdg;
		ReferenceFrame = pReferenceFrame;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}