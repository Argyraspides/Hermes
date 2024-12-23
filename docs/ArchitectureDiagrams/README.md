# Hermes Backend Architecture

## Message Packet Pipeline
Below is a diagram detailing how Hermes will abstract away communication protocols, such that Hermes itself is protocol agnostic

![MsgPacketPipelineDiagram](https://github.com/user-attachments/assets/2e48c58b-d451-44e1-88a1-460f95cf15a1)

Data comes in from the outside world. A world listener module will pick up the data. For now, UDP, TCP, and Serial modules will be supported. Each of these modules will listen in for their respective packet type. E.g., the UDP module plugin for the world listener will listen in on UDP packets coming in.

These packets, now represented in a proper data structure within the program, will be forwarded to a data protocol reckoner, which will determine exactly what kind of protocol it is. Once determined, the packets will be forwarded to a module which will deserialize the packets and form a data structure for the specific data type. For example, the output of the "MAVLink" deserializer might be some sort of MAVLink struct containing all the fields of a typical MAVLink message.

These will then be handed off to a message broker which will signal that a message of a particular protocol has been received. This will essentially be a pub-sub pattern, where any module in the program can subscribe to, say, "MAVLink message received" events. They will also be forwarded to a state converter, where the message of the specific protocol type will be converted to a generic state. 

All vehicles will have some sort of capability state (see the other diagram below for info on this), mission state, or a "core" state. The mission state will indicate any status of a current mission a vehicle may be executing, and core state will have basic physical properties such as the vehicle's position in space, velocity, acceleration, etc.

Once these states are generated, they will be passed to the actual vehicle to update its state. Following proper separation of concerns, the UI can simply bind to the vehicle's state for updates of its own.

## Vehicle Class Architecture
Below is a diagram detailing how Hermes will represent a vehicle 

![Hermes_Vehicle_Architecture](https://github.com/user-attachments/assets/1e63a251-d4dd-40fc-ac50-f347f774879a)

Vehicles can be incredibly varied and highly complex, even amongst the same class of vehicle. An "is a" relationship hence doesn't make sense, and we favor composition ("has a") over inheritance for this particular case. Take for example a glider and a commercial airliner, both of which might inherit from a "Plane" class. You probably want to have some concept of an "Engine" in the base class of plane, maybe with "FuelCapacity" or something, but a glider doesn't have an engine nor does it have any fuel. In fact, it doesn't have anything at all. It's just a giant piece of fibreglass.

All vehicles, being physical entities of the universe, must have a position, attitude, velocity, and acceleration. 

A vehicle can also have a variety of capabilities, which can be added/removed at runtime. Capabilities can be literally anything, from physical things such as whether or not a vehicle has landing gear (``LandingGearCapability``), to behaviors such as whether or not a vehicle is capable of taking off vertically (``VerticalTakeoffCapability``).

Each capability also has its own state. Since capabilities are abstracted in the concrete Vehicle class, a CapabilityType enum can be used to identify them and perform operations, e.g., starting a camera recording.
