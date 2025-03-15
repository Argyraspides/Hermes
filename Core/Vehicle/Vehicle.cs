using Godot;

namespace Hermes.Core.Vehicle;

public partial class Vehicle : RigidBody3D
{
    private Vector3 m_position;
    private Vector3 m_velocity;
    private Vector3 m_acceleration;
    private Vector3 m_attitude;
}
