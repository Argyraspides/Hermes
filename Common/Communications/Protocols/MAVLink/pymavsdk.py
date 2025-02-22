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


#     .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.      #
#    :::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\    #
#    '      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `   #

import json
import asyncio
import websockets

from websockets.server import WebSocketServerProtocol
from threading import Thread
from pymavlink import mavutil
from datetime import datetime
from collections import deque
from typing import Deque, Dict, List, Set, Any, Optional

#     .--.      .-'.      .--.      .--.      .--.      .--.      .`-.      .--.      #
#    :::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\::::::::.\    #
#    '      `--'      `.-'      `--'      `--'      `--'      `-.'      `--'      `   #


deserializedMavlinkMessageBuffer: Deque[str] = deque(maxlen=1024)
mavlinkUdpUrls: List[str] = ["localhost:14550"]

# Static typing ... no matter what
ConnectedClients = Set[WebSocketServerProtocol]
connectedClients: ConnectedClients = set()


# MAVLink Listener uses MAVSDK to listen in on UDP ports for MAVLink messages.
# It then sends these UDP packets to be deserialized into a JSON format,
# and then adds it to the deserializedMavlinkMessageBuffer for the WebSocket
# server to pick up and send it over to the main Hermes application.
class MAVLinkListener:

    def __init__(self):
        self.connections: List[mavutil.mavudp] = []

    def setupUdpConnections(self) -> None:
        for url in mavlinkUdpUrls:
            try:
                connection = mavutil.mavlink_connection(f'udpin:{url}')
                self.connections.append(connection)
            except Exception as e:
                print(f"Failed to connect to {url}: {e}")

    def mavlinkMessageToDictionary(self, msg: Any) -> Dict[str, Any]:

        messageDictionary: Dict[str, Any] = {
            "type": msg.get_type(),
            "msgid": msg.get_msgId(),
            "sysid": msg.get_srcSystem(),
            "compid": msg.get_srcComponent(),
            "timestamp": datetime.now().isoformat(),
        }

        fieldNames = msg.get_fieldnames()
        for field in fieldNames:
            value = getattr(msg, field)
            if isinstance(value, bytes):
                value = value.decode('utf-8', errors='ignore')
            messageDictionary[field] = value

        return messageDictionary

    def listenForMavlinkMessages(self) -> None:

        if not self.connections:
            self.setupUdpConnections()

        while True:
            for connection in self.connections:
                try:
                    msg = connection.recv_match(blocking=False)
                    if msg is not None:

                        # Some MAVLink messages are experimental and marked as "UNKNOWN".
                        # We skip those here.
                        if msg.get_type().find("UNKNOWN") != -1: continue

                        msg_dict: Dict[str, Any] = self.mavlinkMessageToDictionary(msg)

                        json_str = json.dumps(msg_dict)
                        deserializedMavlinkMessageBuffer.append(json_str)

                except Exception as e:
                    print(f"Error processing message: {e}")


# The WebSocketServer creates a websocket server and does one thing:
# take from the deserialized MAVLink message buffer that the MAVLinkListener
# adds to and then broadcasts it to all connected clients
class WebSocketServer:

    def __init__(self):
        self.server: Optional[websockets.WebSocketServer] = None

    """
    TODO: Handle clients later.
    Functionality should involve:
    - Being able to send MAVLink messages to vehicles
    - Being able to add/remove UDP/TCP ports to listen to
    """

    async def handleClient(self, websocket: WebSocketServerProtocol):
        try:
            connectedClients.add(websocket)
            print(f"Client connected. Total clients: {len(connectedClients)}")
            # Keep the connection alive until the client disconnects
            await websocket.wait_closed()
        except Exception as e:
            print(f"Error handling client: {e}")
        finally:
            connectedClients.remove(websocket)
            print(f"Client disconnected. Remaining clients: {len(connectedClients)}")

    """
    TODO: Ensure that this websocket server URL
    can somehow be known outside of this script during runtime
    """

    async def startWebsocketServer(self) -> None:
        self.server = await websockets.serve(
            self.handleClient,
            "localhost",
            8765
        )

    async def publishDeserializedMAVLinkMessages(self) -> None:

        await self.startWebsocketServer()

        while True:
            if connectedClients:
                deserializedMavlinkMessage: str
                try:
                    deserializedMavlinkMessage = deserializedMavlinkMessageBuffer.pop()
                except IndexError:
                    continue

                broadcastTasks: list[asyncio.Task[None]] = [
                    asyncio.create_task(client.send(deserializedMavlinkMessage))
                    for client in connectedClients
                ]
                await asyncio.gather(*broadcastTasks)
            await asyncio.sleep(0.05)  # TODO: Change to like some async handler thingy, dont just constantly poll


if __name__ == "__main__":
    mavlinkListener: MAVLinkListener = MAVLinkListener()
    mavlinkListenerThread: Thread = Thread(target=mavlinkListener.listenForMavlinkMessages)
    mavlinkListenerThread.start()

    webSocketServer: WebSocketServer = WebSocketServer()
    asyncio.run(webSocketServer.publishDeserializedMAVLinkMessages())

    mavlinkListenerThread.join()
