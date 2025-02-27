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

# Queue of MAVLink messages received from the outside world
MAVLinkMessageQueue = deque()
WebSocketURLs = []

# List of UDP, TCP, and serial ports that MAVLinkInterface will listen in on for MAVLink messages
UDPListeningAddresses: [str] = ["localhost:14550"]
TCPListeningAddresses = [str]
SerialListeningAddresses = [str]


class MAVLinkListener:
    def __init__(self):
        pass

    # - Listen for MAVLink messages on a list of UDP/TCP/Serial ports. These lists should be a global variable
    # - Should be event-driven, so no polling
    def StartListenMAVLinkUDP(self):
        pass

    # - Listen for MAVLink messages on a list of UDP/TCP/Serial ports. These lists should be a global variable
    # - Should be event-driven, so no polling
    def StartListenMAVLinkTCP(self):
        pass

    # - Listen for MAVLink messages on a list of UDP/TCP/Serial ports. These lists should be a global variable
    # - Should be event-driven, so no polling
    def StartListenMAVLinkSerial(self):
        pass

    # - Packages the MAVLink message recieved from the StartListenMAVLink function
    #   into a dictionary with all message fields and values filled out (string to Any)
    # - Adds the dictionary to a global dictionary queue
    def PackageMAVLinkMessage(self):
        pass


class MAVLinkSerializer:

    def __init__(self):
        pass

    # - Function to take a MAVLink dictionary message and convert it to a JSON format
    def MAVLinkDictMessageToJSON(self, mavlinkDictMessage):
        pass

    # - Function to take a MAVLink JSON message, and serialize it back into a bytestream (in a way that is suitable to be sent
    # over UDP/TCP/Serial)
    def MAVLinkJSONToByteStream(self, mavlinkJsonMessage):
        pass

    # - Function to take a Hermes state message, and then serialize it into a bytestream (in a way that is suitable to be sent
    # over UDP/TCP/Serial)
    def HermesJSONToByteStream(self, hermesJSONMessage):
        pass

    # These are functions to add and remove TCP/UDP addresses that we want to listen on/no longer want to listen on

    def AddUDPListenAddress(self, udpListenAddress):
        pass

    def AddTCPListenAddress(self, tcpListenAddress):
        pass

    def RemoveUDPListenAddress(self, udpListenAddress):
        pass

    def RemoveTCPListenAddress(self, tcpListenAddress):
        pass


# Python class, "MAVLinkWebSocket" that will

# - Listen for Hermes' requesting to send MAVLink messages to the drones. Any message Hermes sends can be assumed to be a
# message that it wants to send out to a drone
# - Right now it is unclear what format this message will be in. Remember that Hermes is protocol-agnostic. Thus in the interest
# of the cleanest architecture, Hermes will simply send out its "proprietary" internal format as-is. The Python script will have to
# deal with this, serialize it, and send the message out via UDP/TCP/Serial. It might be best to just have Hermes have its own
# module where it can convert a State object to a JSON string, and just hand it over to the Python script like that. This makes
# sense since Hermes knows its own format best, so its better to convert to a JSON over that and then send it here for conversion back
# to MAVLink and deserialization

class MAVLinkWebSocket:

    def __init__(self):
        pass

    def InitializeWebSocket(self, websocketURL):
        pass

    def CloseWebSocket(self, websocketURL):
        pass

    def AddWebSocket(self, websocketURL):
        pass

    def RemoveWebSocket(self, websocketURL):
        pass

    # - Take the next MAVLink dictionary message from the queue
    # - Convert this to a JSON string representation using the MAVLinkSerializer class functions
    # - Send the JSON string over the WebSocket
    def SendMAVLinkJSONToHermes(self):
        pass

# Other things to keep in mind:
# The user will want to configure UDP/TCP/Serial ports they wanna listen in on. The only real way to do this is to have this Python script
# load up a configuration file on startup that reads the different UDP/TCP/Serial ports and loads them in, where this config file will be edited by
# the user, and then Python can pick up on this using a watchdog and update/remove the ports accordingly.
