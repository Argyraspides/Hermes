using System;
using Godot;
using System.Collections.Generic;


/*
This translator converts MAVLink message objects directly to Hellenic message objects.
It works with the C# MAVLink parser in MAVLink.dll to process binary MAVLink data.

Example usage:
MAVLink.MAVLinkMessage mavlinkMsg = new MAVLink.MAVLinkMessage(rawBytes);
List<HellenicMessage> hellenicMessages = MAVLinkToHellenicTranslator.TranslateMAVLinkMessage(mavlinkMsg);
*/
class MAVLinkToHellenicTranslator
{
	public static List<HellenicMessage> TranslateMAVLinkMessage(MAVLink.MAVLinkMessage mavlinkMessage)
	{
		// Extract the message ID
		uint msgId = mavlinkMessage.msgid;
		// Look up the appropriate conversion function
		if (MAVLinkIdToConversionFunctionDict.TryGetValue(msgId, out var conversionFunc))
		{
			return conversionFunc(mavlinkMessage);
		}
		// No suitable translation function found
		Console.WriteLine("Unable to translate MAVLink message! No suitable translation function found for msgid: " + msgId);
		return new List<HellenicMessage>();
	}

	public static List<HellenicMessage> HeartbeatToHellenic(MAVLink.MAVLinkMessage mavlinkMessage)
	{
		// Extract the MAVLink struct from the message object
		var mavlinkData = mavlinkMessage.ToStructure<MAVLink.mavlink_heartbeat_t>();

		var PulseHellenicMessage = new Pulse(
			pMachineId: mavlinkMessage.sysid,
			pOriginalProtocol: (uint)Protocols.Mavlink,
			pMachineType: mavlinkData.type,
			pCallsign: "UNKNOWN CALLSIGN",
			pTimeUsec: (ulong)Time.GetUnixTimeFromSystem()
		);

		return new List<HellenicMessage>
		{
			PulseHellenicMessage
		};
	}

	public static List<HellenicMessage> GlobalPositionIntToHellenic(MAVLink.MAVLinkMessage mavlinkMessage)
	{
		// Extract the MAVLink struct from the message object
		var mavlinkData = mavlinkMessage.ToStructure<MAVLink.mavlink_global_position_int_t>();

		var LatitudeLongitudeHellenicMessage = new LatitudeLongitude(
			pMachineId: mavlinkMessage.sysid,
			pOriginalProtocol: (uint)Protocols.Mavlink,
			pLat: mavlinkData.lat / 10000000.0,
			pLon: mavlinkData.lon / 10000000.0,
			pTimeUsec: mavlinkData.time_boot_ms,
			pReferenceFrame: 2
		);

		var AltitudeHellenicMessage = new Altitude(
			pMachineId: mavlinkMessage.sysid,
			pOriginalProtocol: (uint)Protocols.Mavlink,
			pAlt: mavlinkData.alt / 1000.0,
			pRelativeAlt: mavlinkData.relative_alt / 1000.0,
			pTimeUsec: mavlinkData.time_boot_ms
		);

		var GroundVelocityHellenicMessage = new GroundVelocity(
			pMachineId: mavlinkMessage.sysid,
			pOriginalProtocol: (uint)Protocols.Mavlink,
			pVx: mavlinkData.vx / 100.0,
			pVy: mavlinkData.vy / 100.0,
			pVz: mavlinkData.vz / -100.0,
			pTimeUsec: mavlinkData.time_boot_ms
		);

		var HeadingHellenicMessage = new Heading(
			pMachineId: mavlinkMessage.sysid,
			pOriginalProtocol: (uint)Protocols.Mavlink,
			pHdg: mavlinkData.hdg / 100.0,
			pTimeUsec: mavlinkData.time_boot_ms,
			pReferenceFrame: 2
		);

		return new List<HellenicMessage>
		{
			LatitudeLongitudeHellenicMessage,
			AltitudeHellenicMessage,
			GroundVelocityHellenicMessage,
			HeadingHellenicMessage
		};
	}

	public static Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>>
		MAVLinkIdToConversionFunctionDict
			=
			new Dictionary<uint, Func<MAVLink.MAVLinkMessage, List<HellenicMessage>>>()
			{
				{0, HeartbeatToHellenic},
				{33, GlobalPositionIntToHellenic}
			};
}