/*

                                       


88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba  
88        88  88           88      "8b  888b         d888  88          d8"     "8b 
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,         
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,   
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b, 
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b 
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P 
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"  


                            MESSENGER OF THE MACHINES

*/


using Godot;
using System.Text;
using System.Text.Json;
public partial class MAVLinkDeserializer : Node
{



	[Signal]
	public delegate void MAVLinkJsonMessageReceivedEventHandler(JsonWrapper mavlinkJsonMessage);


	private void DeserializeMAVLinkJsonPacket(byte[] rawMAVLinkJsonPacket)
	{
		if (rawMAVLinkJsonPacket == null) return;
		if (rawMAVLinkJsonPacket.Length == 0) return;

		string deserializedMAVLinkJsonMessage = Encoding.UTF8.GetString(rawMAVLinkJsonPacket, 0, rawMAVLinkJsonPacket.Length);

		deserializedMAVLinkJsonMessage = deserializedMAVLinkJsonMessage.Replace("NaN", "null");

		try
		{
			var jsonObject = JsonSerializer.Deserialize<JsonElement>(deserializedMAVLinkJsonMessage);
			JsonWrapper jsonWrapper = new JsonWrapper();
			jsonWrapper.Data = jsonObject;

			EmitSignal("MAVLinkJsonMessageReceived", jsonWrapper);
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"Failed to parse MAVLink JSON message: {ex.Message}");
		}
	}

	// TODO: What we have here is dangerous. We are assuming that any incoming WebSocket packet is a serialized MAVLink
	// JSON packet. Here, we should instead:
	//
	// 1: Check if the packet is a MAVLink one at all
	// 2: Check whether it is a JSON MAVLink packet, or a "raw" MAVLink packet
	// 3: Forward the packet to the correct deserializer function
	// As it stands, the 'pymavsdk.py' Python script already deserializes any MAVLink messages
	// into a JSON format, so this is OK for now.
	private void onWebSocketPacketReceived(byte[] websocketPacket)
	{
		DeserializeMAVLinkJsonPacket(websocketPacket);
	}

	public override void _Ready()
	{
		// TODO: Getting the WorldListener node this way is fragile and breaks the moment
		// we want to restructure the scene tree. Find a robust way to do it instead!
		var worldListenerNode = GetNode<WorldListener>("../../WorldListener");
		worldListenerNode.WebSocketPacketReceived += onWebSocketPacketReceived;
	}


}
