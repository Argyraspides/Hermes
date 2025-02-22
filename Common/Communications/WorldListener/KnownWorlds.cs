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

namespace Hermes.Common.Communications.WorldListener;

// TODO: Add a description on what exactly this is
public static class KnownWorlds
{
    // 14550 is the default port that MAVLink packets are sent to from pretty much all PX4/ArduPilot devices
    public const int DEFAULT_MAVLINK_PORT = 14550;

    // This is the default websocket URL that the MAVSDK Python script which listens in on
    // MAVLink packets ("pymavsdk.py") sends deserialized MAVLink messages to
    public const string DEFAULT_WEBSOCKET_URL = "ws://localhost:8765";
}
