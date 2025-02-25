#
#
#
#
# 88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
# 88        88  88           88      "8b  888b         d888  88          d8"     "8b
# 88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
# 88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
# 88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
# 88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
# 88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
# 88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"
#
#
#                              MESSENGER OF THE MACHINES
#

import json
import asyncio
import websockets

from websockets.server import WebSocketServerProtocol
from threading import Thread
from pymavlink import mavutil
from datetime import datetime
from collections import deque
from typing import Deque, Dict, List, Set, Any, Optional

# Python class, "MAVLinkListener" that will:
# - Listen for MAVLink messages on a list of UDP/TCP/Serial ports. These lists should be a global variable
# - Should be event-driven, so no polling
# - Packages the MAVLink message into a dictionary with all message fields and values filled out (string to Any)
# - Adds the dictionary to a global dictionary queue

# Python class, "MAVLinkSerializer" that has the following functions:
# - Function to take a MAVLink dictionary message and convert it to a JSON format
# - Function to take a MAVLink JSON message, and serialize it back into a bytestream (in a way that is suitable to be sent
# over UDP/TCP/Serial)

# Python class, "MAVLinkWebSocket" that will
# - Initialize a WebSocket immediately. The URL should be a global variable
# - Take the next MAVLink dictionary message from the queue
# - Convert this to a JSON string representation
# - Send the JSON string over the WebSocket

# Other things to keep in mind:
# The user will want to configure UDP/TCP/Serial ports they wanna listen in on. The only real way to do this is to either have this Python script
# load up some config file on startup that reads the different UDP/TCP/Serial ports and loads them in, where this config file will be edited by
# the user, requiring an application restart, OR Hermes can utilize the WebSocket to send messages that it wants to add/remove UDP/TCP/Serial
# ports and whatnot to listen in on.
