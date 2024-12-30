# Hermes Backend Architecture

## Message Packet Pipeline
Below is a diagram detailing how Hermes will abstract away communication protocols, such that Hermes itself is protocol agnostic

![MsgPacketPipelineDiagram](https://github.com/user-attachments/assets/1316d253-d53c-4a0c-bbf0-5adc09d5df89)


Data comes in from the outside world. A world listener module will pick up the data. For now, UDP, TCP, and Serial modules will be supported. Each of these modules will listen in for their respective packet type. E.g., the UDP module plugin for the world listener will listen in on UDP packets coming in.

These packets, now represented in a proper raw-byte data structure within the program, will be forwarded to its respective deserializer, which will deserialize the packets and form a data structure for the specific data type. For example, the output of the "MAVLink" deserializer is currently a sort of MAVLink data object containing all the fields of a typical MAVLink message as a JSON.

These deserialized messages will then be forwarded to a state converter for the respective protocol. The job of these state converters is to convert protocol-specific states into a generic, in-game vehicle state. At this point, Hermes becomes completely protocol agnostic. 

All vehicles will have some sort of capability state (see the other diagram below for info on this), mission state, or a "core" state. The mission state will indicate any status of a current mission a vehicle may be executing, and core state will have basic physical and identifier properties such as the vehicle's position in space, velocity, acceleration, unique vehicle ID, vehicle type, etc.

Once these states are generated, they will be passed to a message broker where any component may subscribe to in order to obtain vehicle states. For now, the only listening module is the MultiVehicleManager, whose sole purpose is to manage the lifecycle of any vehicles by adding them to the map scene tree, removing them from the map scene tree, and updating their state. 

Hermes UI will allow commanding the vehicles. Each vehicle can be controlled by invoking one or more of its capabilities. Abstracting away protocols again, the specific protocol type will be automatically determined and handed off to a specific commander module, which will then serialize the message and send it to the vehicle. 

Strengths:
- Abstract away all protocol types
- Incredibly simple and linear flow, hence easy to follow and reason about events (great for debuggability)
- Incredibly modular and flexible to changes
- Very loose coupling. Much of the communication within Godot will happen via signalling
- Data flow for receiving information is one way, as is communication to the UxVs. No propogation in the opposite direction means that vehicle state is a true depiction of reality with zero chances of 

Weaknesses:
- Lot of developer "overhead" to get protcol abstraction up and running, as entire state converters need to be created
- Any firmware differences such as between PX4 and Ardupilot are not accounted for. For now this is mitigated by using MAVSDK for the MAVLink implementation. Firmware plugin system may need to be created similar to what QGroundControl has in the future
- 

## Vehicle Class Architecture
Below is a diagram detailing how Hermes will represent a vehicle 

![Hermes_Vehicle_Architecture](https://github.com/user-attachments/assets/1e63a251-d4dd-40fc-ac50-f347f774879a)

Vehicles can be incredibly varied and highly complex, even amongst the same class of vehicle. An "is a" relationship hence doesn't make sense, and we favor composition ("has a") over inheritance for this particular case. Take for example a glider and a commercial airliner, both of which might inherit from a "Plane" class. You probably want to have some concept of an "Engine" in the base class of plane, maybe with "FuelCapacity" or something, but a glider doesn't have an engine nor does it have any fuel. In fact, it doesn't have anything at all. It's just a giant piece of fibreglass.

All vehicles, being physical entities of the universe, must have a position, attitude, velocity, and acceleration. 

A vehicle can also have a variety of capabilities, which can be added/removed at runtime. Capabilities can be literally anything, from physical things such as whether or not a vehicle has landing gear (``LandingGearCapability``), to behaviors such as whether or not a vehicle is capable of taking off vertically (``VerticalTakeoffCapability``).

Each capability also has its own state. Since capabilities are abstracted in the concrete Vehicle class, a CapabilityType enum can be used to identify them and perform operations, e.g., starting a camera recording.

## Map Tile API Architecture
Below is a diagram detailing how Hermes obtains geospatial data (specifically map tiles)

![Hermes_Map_API_Architecture](https://github.com/user-attachments/assets/72b2bf1d-c1e2-43b4-8508-e1afc03a4e80)

The MapAPI is basically an interface to using the map providers to get tiles. There are example comments in the MapAPI.cs file on how to use it. You can simply use the RequestMapTile() function in the MapAPI to request a map tile at a particular longitude, latitude, and zoom level. The request is sent to an abstract MapProvider class which can be of type BingMapProvider, GoogleMapProvider, etc., to handle fetching map tiles from a particular provider. The flow from there on in is very simple: A map tile is requested by first constructing the necessary query parameters from the latitude, longitude, and desired zoom level, inserting these query parameters to the complete URL template to construct the final query string, then this is given to actually fetch the raw map tile data.

Concurrent HTTP requests are supported. The FetchRawMapTileData function will try to get one of the HTTP requester objects. If one is available, it will fire an HTTP request using it with the query string. If an HTTP requester is unavailable, it will just queue it up in what is currently a ConcurrentQueue<HttpRequest> object. This queue is constantly processed in the _Process(). Similarly if an HTTP request fails, we queue up a call to FetchRawMapTileData again to try once more.

If an HTTP request is successful, a signal will be emitted carrying the raw image data payload. This signal is already connected to MapAPI.cs, and will automatically convert this to a Texture2D and emit another signal of its own which can be connected to any custom handler function to then decide what to do with the Texture2D. Since multiple HTTP request objects may signal at the same time, we avoid the lost sigmal problem by queuing up the signals and then processing them every engine loop in the _Process() function.
