import asyncio
import json
from pymavlink import mavutil
from datetime import datetime
import websockets
import logging


class MAVLinkListener:
    def __init__(self, connection_string):
        """
        Initialize MAVLink connection

        Args:
            connection_string (str): Connection string for MAVLink
                (e.g., 'udpin:localhost:14550' for UDP)
        """
        self.connection = mavutil.mavlink_connection(connection_string)

    def message_to_dict(self, msg):
        """Convert MAVLink message to dictionary format"""
        # Previous implementation remains the same
        field_names = msg.get_fieldnames()
        message_dict = {
            "type": msg.get_type(),
            "msgid": msg.get_msgId(),
            "sysid": msg.get_srcSystem(),
            "compid": msg.get_srcComponent(),
            "timestamp": datetime.now().isoformat(),
        }
        for field in field_names:
            if hasattr(msg, field):
                value = getattr(msg, field)
                if isinstance(value, bytes):
                    value = value.decode('utf-8', errors='ignore')
                message_dict[field] = value
        return message_dict

    async def listen(self, websocket, message_types=None):
        """
        Listen for MAVLink messages and send them over WebSocket

        Args:
            websocket: WebSocket connection object
            message_types (list, optional): List of specific message types to listen for.
                If None, listen for all messages.
        """
        while True:
            msg = self.connection.recv_match(blocking=True)

            if msg is None:
                continue

            if msg.get_type() == "BAD_DATA":
                continue

            if message_types and msg.get_type() not in message_types:
                continue

            message_dict = self.message_to_dict(msg)
            json_message = json.dumps(message_dict)

            try:
                await websocket.send(json_message)
            except websockets.exceptions.ConnectionClosed:
                logging.info("WebSocket connection closed")
                break
            except Exception as e:
                logging.error(f"Error sending message: {e}")
                continue

            await asyncio.sleep(0.001)


async def websocket_server(websocket, path):
    """Handle incoming WebSocket connections"""
    connection_string = 'udpin:localhost:14550'  # Modify as needed
    message_types = ['ATTITUDE', 'GLOBAL_POSITION_INT']

    listener = MAVLinkListener(connection_string)
    logging.info(f"New WebSocket connection established. Listening for MAVLink messages on {connection_string}")

    try:
        await listener.listen(websocket, message_types)
    except Exception as e:
        logging.error(f"Error in WebSocket server: {e}")


async def main():
    # Configure logging
    logging.basicConfig(level=logging.INFO)

    # Start WebSocket server
    server = await websockets.serve(
        websocket_server,
        "localhost",
        8765  # WebSocket port
    )

    logging.info("WebSocket server started on ws://localhost:8765")

    try:
        await server.wait_closed()
    except KeyboardInterrupt:
        logging.info("Shutting down server...")


if __name__ == "__main__":
    asyncio.run(main())
