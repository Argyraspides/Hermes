using System.Collections.Generic;
using Godot;

namespace Hermes.Core.Vehicle;

public partial class Vehicle : RigidBody3D
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;
    public Vector3 Altitude;

    public Dictionary<ComponentType, Component> Components = new Dictionary<ComponentType, Component>();
}
