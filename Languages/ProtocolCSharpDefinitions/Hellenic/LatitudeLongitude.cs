/// <summary>
/// The position of the object expressed in latitude, 
/// longitude, and altitude,  supporting high-precision planetary and space 
/// navigation.   
/// </summary>
partial class LatitudeLongitude : HellenicMessage
{

	/// <summary>
	/// Latitude (WGS84 or planetary model) 
	/// </summary>
	public double? Lat { get; set; }


	/// <summary>
	/// Longitude (WGS84 or planetary model) 
	/// </summary>
	public double? Lon { get; set; }


	/// <summary>
	/// Reference frame (0 = Mercury, 1 = Venus, 
	/// 2 = Earth, 3 = Moon, 4 =  
	/// Mars ...)   
	/// </summary>
	public byte? ReferenceFrame { get; set; }


	/// <summary>
	/// Timestamp (microseconds since epoch) 
	/// </summary>
	public ulong? TimeUsec { get; set; }

	public LatitudeLongitude()
	{
		Id = 0;
	}


	public LatitudeLongitude(
		uint pMachineId,
		uint pOriginalProtocol,
		double pLat,
		double pLon,
		byte pReferenceFrame,
		ulong pTimeUsec
	)
	{
		Id = 0;
		Lat = pLat;
		Lon = pLon;
		ReferenceFrame = pReferenceFrame;
		TimeUsec = pTimeUsec;
		MachineId = pMachineId;
		OriginalProtocol = pOriginalProtocol;
	}

}
