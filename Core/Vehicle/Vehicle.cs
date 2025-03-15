using System.Collections.Generic;
using Godot;
using Hermes.Core.Vehicle.Components;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Core.Vehicle;

public partial class Vehicle : RigidBody3D
{
    public Dictionary<ComponentType, Component> Components = new Dictionary<ComponentType, Component>();

    public override void _Ready()
    {
    }
}
