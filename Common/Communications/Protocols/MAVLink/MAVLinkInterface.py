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
import os
import time
from threading import Thread
from pymavlink import mavutil
from datetime import datetime
from collections import deque
from typing import Deque, Dict, List, Set, Any, Optional
from websockets.server import WebSocketServerProtocol

# TODO(Argyraspides, 01/03/2025): This should undergo strict and robust testing.

# Queue of MAVLink messages received from the outside world
MAVLinkMessageQueue = deque()

# Queue of MAVLink commands given to us by Hermes
MAVLinkCommandQueue = deque()

# WebSocket URLs for communication with Hermes
WebSocketURLs = ["localhost:8765"]

# List of UDP, TCP, and serial ports that MAVLinkInterface will listen in on for MAVLink messages
UDPListeningAddresses: List[str] = ["localhost:14550"]
TCPListeningAddresses: List[str] = []
SerialListeningAddresses: List[str] = []


class MAVLinkListener:
    def __init__(self):
        self.udp_connections = []
        self.tcp_connections = []
        self.serial_connections = []
        self.running = False
        self.threads = []

    def StartListenMAVLinkUDP(self):
        """
        Listen for MAVLink messages on UDP ports defined in UDPListeningAddresses.
        Starts a new thread for each connection.
        """
        for address in UDPListeningAddresses:
            thread = Thread(target=self._listen_udp, args=(address,))
            thread.daemon = True
            thread.start()
            self.threads.append(thread)
            print(f"Started UDP listener on {address}")

    def StartListenMAVLinkTCP(self):
        """
        Listen for MAVLink messages on TCP ports defined in TCPListeningAddresses.
        Starts a new thread for each connection.
        """
        for address in TCPListeningAddresses:
            thread = Thread(target=self._listen_tcp, args=(address,))
            thread.daemon = True
            thread.start()
            self.threads.append(thread)
            print(f"Started TCP listener on {address}")

    def StartListenMAVLinkSerial(self):
        """
        Listen for MAVLink messages on serial ports defined in SerialListeningAddresses.
        Starts a new thread for each connection.
        """
        for address in SerialListeningAddresses:
            thread = Thread(target=self._listen_serial, args=(address,))
            thread.daemon = True
            thread.start()
            self.threads.append(thread)
            print(f"Started Serial listener on {address}")

    def _listen_udp(self, address):
        """
        Internal method to listen for MAVLink messages on a UDP connection.
        """
        try:
            # Parse address (host:port)
            host, port = address.split(':')
            port = int(port)

            # Create UDP connection
            connection = mavutil.mavlink_connection(f'udpin:{host}:{port}')
            self.udp_connections.append(connection)

            # Listen for messages in a loop
            while self.running:
                msg = connection.recv_match(blocking=True, timeout=1.0)
                if msg:
                    self.PackageMAVLinkMessage(msg)
        except Exception as e:
            print(f"Error in UDP listener for {address}: {e}")

    def _listen_tcp(self, address):
        """
        Internal method to listen for MAVLink messages on a TCP connection.
        """
        try:
            # Parse address (host:port)
            host, port = address.split(':')
            port = int(port)

            # Create TCP connection
            connection = mavutil.mavlink_connection(f'tcp:{host}:{port}')
            self.tcp_connections.append(connection)

            # Listen for messages in a loop
            while self.running:
                msg = connection.recv_match(blocking=True, timeout=1.0)
                if msg:
                    self.PackageMAVLinkMessage(msg)
        except Exception as e:
            print(f"Error in TCP listener for {address}: {e}")

    def _listen_serial(self, address):
        """
        Internal method to listen for MAVLink messages on a serial connection.
        """
        try:
            # Create serial connection
            connection = mavutil.mavlink_connection(address)
            self.serial_connections.append(connection)

            # Listen for messages in a loop
            while self.running:
                msg = connection.recv_match(blocking=True, timeout=1.0)
                if msg:
                    self.PackageMAVLinkMessage(msg)
        except Exception as e:
            print(f"Error in Serial listener for {address}: {e}")

    def PackageMAVLinkMessage(self, msg):
        """
        Packages a MAVLink message into a dictionary and adds it to the global message queue.

        Args:
            msg: A MAVLink message object from pymavlink
        """
        # Skip 'bad data' messages
        if msg.get_type() == 'BAD_DATA':
            return

        # Convert message to dictionary
        msg_dict = {
            "msgid": msg.get_msgId(),
            "sysid": msg.get_srcSystem(),
            "compid": msg.get_srcComponent(),
            "sequence": msg.get_seq(),
            "payload": msg.to_dict(),
        }

        MAVLinkMessageQueue.append(msg_dict)

    def start(self):
        """
        Start all listeners.
        """
        self.running = True
        self.StartListenMAVLinkUDP()
        self.StartListenMAVLinkTCP()
        self.StartListenMAVLinkSerial()

    def stop(self):
        """
        Stop all listeners.
        """
        self.running = False
        # Wait for all threads to terminate
        for thread in self.threads:
            thread.join(timeout=2.0)
        self.threads = []


class MAVLinkSpeaker:

    def __init__(self):
        self.connections = {}  # Map of system_id -> connection
        self.running = False
        self.listener = None  # Will be set in main function
        self.thread = None

    def set_listener(self, listener):
        """
        Set the MAVLinkListener instance to access connections.
        """
        self.listener = listener

    def SendNextCommand(self):
        """
        Takes the next MAVLink command from the queue and sends it to the correct drone.
        Returns True if a command was sent, False otherwise.
        """
        if not MAVLinkCommandQueue:
            return False

        command = MAVLinkCommandQueue.popleft()

        # Get target system ID and component ID
        target_system = command.get('target_system', 1)
        target_component = command.get('target_component', 0)

        # Find or create a connection
        connection = self._get_connection(target_system)
        if not connection:
            print(f"No connection available for system ID {target_system}")
            return False

        # Process command based on type
        command_type = command.get('type')
        if not command_type:
            print("Command missing 'type' field")
            return False

        try:
            if command_type == 'COMMAND_LONG':
                params = command.get('params', {})
                connection.mav.command_long_send(
                    target_system,
                    target_component,
                    params.get('command'),
                    params.get('confirmation', 0),
                    params.get('param1', 0.0),
                    params.get('param2', 0.0),
                    params.get('param3', 0.0),
                    params.get('param4', 0.0),
                    params.get('param5', 0.0),
                    params.get('param6', 0.0),
                    params.get('param7', 0.0)
                )
            elif command_type == 'SET_MODE':
                params = command.get('params', {})
                connection.mav.set_mode_send(
                    target_system,
                    params.get('base_mode', 0),
                    params.get('custom_mode', 0)
                )
            elif command_type == 'PARAM_SET':
                params = command.get('params', {})
                param_id = params.get('param_id', '')
                param_value = params.get('param_value', 0.0)
                param_type = params.get('param_type', 0)
                connection.mav.param_set_send(
                    target_system,
                    target_component,
                    param_id.encode('utf-8'),
                    param_value,
                    param_type
                )
            else:
                print(f"Unknown command type: {command_type}")
                return False

            return True
        except Exception as e:
            print(f"Error sending command: {e}")
            return False

    def _get_connection(self, system_id):
        """
        Get or create a connection for the given system ID.
        """
        if not self.listener:
            print("No MAVLinkListener set")
            return None

        # Check if we already have a connection for this system
        if system_id in self.connections:
            return self.connections[system_id]

        # Look for a connection with matching system ID
        all_connections = (
                self.listener.udp_connections +
                self.listener.tcp_connections +
                self.listener.serial_connections
        )

        for conn in all_connections:
            if hasattr(conn, 'target_system') and conn.target_system == system_id:
                self.connections[system_id] = conn
                return conn

        # No specific connection found, use the first available
        if all_connections:
            conn = all_connections[0]
            self.connections[system_id] = conn
            return conn

        return None

    def _command_loop(self):
        """
        Process commands from the queue in a loop.
        """
        while self.running:
            # Process next command if available
            if not self.SendNextCommand():
                # Sleep briefly if no commands to process
                time.sleep(0.01)

    def Start(self):
        """
        Starts the MAVLink speaker event loop.
        """
        self.running = True
        self.thread = Thread(target=self._command_loop)
        self.thread.daemon = True
        self.thread.start()
        print("MAVLinkSpeaker started")

    def stop(self):
        """
        Stops the MAVLink speaker.
        """
        self.running = False
        if self.thread:
            self.thread.join(timeout=2.0)
            self.thread = None


class MAVLinkSerializer:

    def __init__(self):
        pass

    # def MAVLinkDictMessageToJSON(self, mavlink_dict_message):
    #     """
    #     Function to take a MAVLink dictionary message and convert it to a JSON format.

    #     Args:
    #         mavlink_dict_message: Dictionary containing MAVLink message data

    #     Returns:
    #         JSON string representation of the message
    #     """
    #     jsonDump = json.dumps(mavlink_dict_message)
    #     return jsonDump

    def MAVLinkDictMessageToJSON(self, mavlink_dict_message):
        """
        Function to take a MAVLink dictionary message and convert it to a JSON format.

        Args:
            mavlink_dict_message: Dictionary containing MAVLink message data

        Returns:
            JSON string representation of the message
        """
        try:
            return json.dumps(mavlink_dict_message)
        except TypeError as e:
            # Find the problematic field(s)
            problematic_fields = {}
            for key, value in mavlink_dict_message.items():
                try:
                    json.dumps({key: value})
                except TypeError:
                    problematic_fields[key] = f"{type(value).__name__}: {str(value)[:50]}"

        print(f"JSON serialization error: {e}")
        print(f"Problematic fields: {problematic_fields}")

    def MAVLinkJSONToByteStream(self, mavlink_json_message):
        """
        Function to take a MAVLink JSON message, and serialize it back into a bytestream
        (in a way that is suitable to be sent over UDP/TCP/Serial).

        Args:
            mavlink_json_message: JSON string containing MAVLink message data

        Returns:
            Bytes to be sent over a MAVLink connection
        """
        try:
            # Parse JSON
            msg_dict = json.loads(mavlink_json_message)

            # Extract message type and fields
            msg_type = msg_dict.get('type')
            fields = msg_dict.get('fields', {})

            # This would require detailed handling for each message type
            # In a real implementation, this would map JSON fields to MAVLink message fields
            # For example:
            # if msg_type == 'HEARTBEAT':
            #     return mav.heartbeat_send(
            #         fields.get('type', 0),
            #         fields.get('autopilot', 0),
            #         fields.get('base_mode', 0),
            #         fields.get('custom_mode', 0),
            #         fields.get('system_status', 0)
            #     ).pack()

            print(f"MAVLinkJSONToByteStream: message type {msg_type} not yet implemented")
            return None
        except Exception as e:
            print(f"Error converting JSON to bytestream: {e}")
            return None

    def HermesJSONToByteStream(self, hermes_json_message):
        """
        Function to take a Hermes JSON message, and then serialize it into a bytestream
        (in a way that is suitable to be sent over UDP/TCP/Serial).

        Args:
            hermes_json_message: JSON string containing Hermes command data

        Returns:
            Bytes to be sent over a MAVLink connection
        """
        # Similar to MAVLinkJSONToByteStream but might have Hermes-specific handling
        return self.MAVLinkJSONToByteStream(hermes_json_message)

    def AddUDPListenAddress(self, udp_listen_address):
        """
        Add a UDP address to the list of addresses to listen on.

        Args:
            udp_listen_address: String in format "host:port"
        """
        global UDPListeningAddresses
        if udp_listen_address not in UDPListeningAddresses:
            UDPListeningAddresses.append(udp_listen_address)
            print(f"Added UDP listen address: {udp_listen_address}")

    def AddTCPListenAddress(self, tcp_listen_address):
        """
        Add a TCP address to the list of addresses to listen on.

        Args:
            tcp_listen_address: String in format "host:port"
        """
        global TCPListeningAddresses
        if tcp_listen_address not in TCPListeningAddresses:
            TCPListeningAddresses.append(tcp_listen_address)
            print(f"Added TCP listen address: {tcp_listen_address}")

    def RemoveUDPListenAddress(self, udp_listen_address):
        """
        Remove a UDP address from the list of addresses to listen on.

        Args:
            udp_listen_address: String in format "host:port"
        """
        global UDPListeningAddresses
        if udp_listen_address in UDPListeningAddresses:
            UDPListeningAddresses.remove(udp_listen_address)
            print(f"Removed UDP listen address: {udp_listen_address}")

    def RemoveTCPListenAddress(self, tcp_listen_address):
        """
        Remove a TCP address from the list of addresses to listen on.

        Args:
            tcp_listen_address: String in format "host:port"
        """
        global TCPListeningAddresses
        if tcp_listen_address in TCPListeningAddresses:
            TCPListeningAddresses.remove(tcp_listen_address)
            print(f"Removed TCP listen address: {tcp_listen_address}")


class MAVLinkWebSocket:

    def __init__(self):
        self.serializer = MAVLinkSerializer()
        self.websocket_servers = {}  # Maps URL to server object
        self.active_clients = set()
        self.running = False
        self.message_task = None

    async def InitializeWebSocket(self, websocket_url):
        """
        Initialize a WebSocket server at the given URL.

        Args:
            websocket_url: String in format "host:port"
        """
        try:
            # Parse host and port
            host, port = websocket_url.split(':')
            port = int(port)

            # Start WebSocket server
            server = await websockets.serve(
                self._handle_client,
                host,
                port
            )

            self.websocket_servers[websocket_url] = server
            print(f"WebSocket server started at {websocket_url}")
            return True
        except Exception as e:
            print(f"Error initializing WebSocket at {websocket_url}: {e}")
            return False

    async def CloseWebSocket(self, websocket_url):
        """
        Close a WebSocket server.

        Args:
            websocket_url: String in format "host:port"
        """
        if websocket_url in self.websocket_servers:
            server = self.websocket_servers.pop(websocket_url)
            server.close()
            await server.wait_closed()
            print(f"WebSocket server closed at {websocket_url}")
            return True
        return False

    async def AddWebSocket(self, websocket_url):
        """
        Add a WebSocket URL to the list and initialize it.

        Args:
            websocket_url: String in format "host:port"
        """
        global WebSocketURLs
        if websocket_url not in WebSocketURLs:
            WebSocketURLs.append(websocket_url)
            await self.InitializeWebSocket(websocket_url)

    async def RemoveWebSocket(self, websocket_url):
        """
        Remove a WebSocket URL from the list and close it.

        Args:
            websocket_url: String in format "host:port"
        """
        global WebSocketURLs
        if websocket_url in WebSocketURLs:
            WebSocketURLs.remove(websocket_url)
            await self.CloseWebSocket(websocket_url)

    async def _handle_client(self, websocket: WebSocketServerProtocol):
        """
        Handle a WebSocket client connection.

        Args:
            websocket: WebSocket connection object
            path: URL path
        """
        # Add to active clients
        self.active_clients.add(websocket)

        try:
            # Handle incoming messages
            async for message in websocket:
                try:
                    # Parse message as JSON
                    command = json.loads(message)

                    # Add to command queue
                    MAVLinkCommandQueue.append(command)
                    print(f"Received command: {command.get('type', 'unknown')}")
                except json.JSONDecodeError:
                    print(f"Received invalid JSON: {message}")
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket connection closed")
        finally:
            # Remove from active clients
            self.active_clients.remove(websocket)

    async def SendMAVLinkJSONToHermes(self):
        """
        Take the next MAVLink dictionary message from the queue,
        convert it to JSON, and send it over the WebSocket.

        Returns True if a message was sent, False otherwise.
        """
        if not MAVLinkMessageQueue:
            return False

        # Get next message
        msg_dict = MAVLinkMessageQueue.popleft()

        # Convert to JSON
        json_msg = self.serializer.MAVLinkDictMessageToJSON(msg_dict)

        # Send to all connected clients
        await self._send_to_all_clients(json_msg)
        return True

    async def _send_to_all_clients(self, message):
        """
        Send a message to all connected WebSocket clients.

        Args:
            message: Message to send
        """
        if not self.active_clients:
            return

        # Send to all clients (handle disconnections)
        disconnected = set()
        for websocket in self.active_clients:
            try:
                await websocket.send(message)
            except websockets.exceptions.ConnectionClosed:
                disconnected.add(websocket)

        # Remove disconnected clients
        for websocket in disconnected:
            self.active_clients.remove(websocket)

    async def _process_message_queue(self):
        """
        Process MAVLink messages from the queue and send them to Hermes.
        """
        while self.running:
            try:
                # Send next message if available
                if not await self.SendMAVLinkJSONToHermes():
                    # Sleep briefly if no messages to process
                    await asyncio.sleep(0.01)
            except Exception as e:
                print(f"Error processing message queue: {e}")
                await asyncio.sleep(1)  # Sleep longer on error

    async def start(self):
        """
        Start the WebSocket servers and message processing.
        """
        self.running = True

        # Start WebSocket servers
        for url in WebSocketURLs:
            await self.InitializeWebSocket(url)

        # Start message processing
        self.message_task = asyncio.create_task(self._process_message_queue())
        print("MAVLinkWebSocket started")

    async def stop(self):
        """
        Stop the WebSocket servers and message processing.
        """
        self.running = False

        # Cancel message processing task
        if self.message_task:
            self.message_task.cancel()
            try:
                await self.message_task
            except asyncio.CancelledError:
                pass

        # Close all WebSocket servers
        for url in list(self.websocket_servers.keys()):
            await self.CloseWebSocket(url)


class ConfigManager:
    """
    Manages configuration loading and watching for changes.
    """

    def __init__(self, config_file_path):
        self.config_file_path = config_file_path
        self.last_modified = 0

    def load_config(self):
        """
        Load configuration from file.

        Returns:
            bool: True if config loaded successfully, False otherwise
        """
        try:
            # Check if file exists
            if not os.path.exists(self.config_file_path):
                print(f"Config file not found: {self.config_file_path}")
                return False

            # Load JSON config
            with open(self.config_file_path, 'r') as f:
                config = json.load(f)

            # Update global variables
            global UDPListeningAddresses
            global TCPListeningAddresses
            global SerialListeningAddresses
            global WebSocketURLs

            UDPListeningAddresses = config.get('udp_addresses', UDPListeningAddresses)
            TCPListeningAddresses = config.get('tcp_addresses', TCPListeningAddresses)
            SerialListeningAddresses = config.get('serial_addresses', SerialListeningAddresses)
            WebSocketURLs = config.get('websocket_urls', WebSocketURLs)

            self.last_modified = os.path.getmtime(self.config_file_path)
            print(f"Configuration loaded from {self.config_file_path}")
            return True
        except Exception as e:
            print(f"Error loading configuration: {e}")
            return False

    def check_for_changes(self):
        """
        Check if the config file has changed.

        Returns:
            bool: True if file changed and was reloaded, False otherwise
        """
        try:
            if not os.path.exists(self.config_file_path):
                return False

            mtime = os.path.getmtime(self.config_file_path)
            if mtime > self.last_modified:
                return self.load_config()

            return False
        except Exception as e:
            print(f"Error checking config file: {e}")
            return False

    def create_default_config(self):
        """
        Create a default configuration file if it doesn't exist.
        """
        if os.path.exists(self.config_file_path):
            return

        try:
            config = {
                'udp_addresses': UDPListeningAddresses,
                'tcp_addresses': TCPListeningAddresses,
                'serial_addresses': SerialListeningAddresses,
                'websocket_urls': WebSocketURLs
            }

            with open(self.config_file_path, 'w') as f:
                json.dump(config, f, indent=4)

            print(f"Created default config file: {self.config_file_path}")
        except Exception as e:
            print(f"Error creating default config: {e}")


async def config_watcher(config_manager, listener, speaker, websocket):
    """
    Periodically check for config file changes and restart components if needed.
    """
    while True:
        if config_manager.check_for_changes():
            # Configuration changed, restart components
            print("Configuration changed, restarting components")

            # Stop components
            listener.stop()
            speaker.stop()
            await websocket.stop()

            # Start components
            listener.start()
            speaker.Start()
            await websocket.start()

        # Check every 5 seconds
        # TODO(Argyraspides, 02/03/2025): Ideally this should be event based
        await asyncio.sleep(5)


async def main():
    """
    Main function to start the MAVLink proxy.
    """
    print("Starting MAVLink proxy for Hermes")

    # Create config manager
    config_file_path = "hermes_config.json"
    config_manager = ConfigManager(config_file_path)
    config_manager.create_default_config()
    config_manager.load_config()

    # Initialize components
    listener = MAVLinkListener()
    speaker = MAVLinkSpeaker()
    websocket = MAVLinkWebSocket()

    # Set up component relationships
    speaker.set_listener(listener)

    # Start config watcher
    asyncio.create_task(config_watcher(config_manager, listener, speaker, websocket))

    # Start components
    print("Starting MAVLink components")
    listener.start()
    speaker.Start()
    await websocket.start()

    # Keep running until interrupted
    try:
        print("MAVLink proxy running. Press Ctrl+C to exit.")
        while True:
            await asyncio.sleep(1)
    except KeyboardInterrupt:
        print("Shutting down MAVLink proxy")
    finally:
        # Stop components
        listener.stop()
        speaker.stop()
        await websocket.stop()


if __name__ == "__main__":
    asyncio.run(main())
