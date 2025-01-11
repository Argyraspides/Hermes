using Godot;
using System;

public partial class SolarSystem : Node3D
{

	StaticBody3D earth;
	Camera3D camera3D;

	public override void _Ready()
	{
		earth = GetNode<StaticBody3D>("Earth");
		camera3D = GetNode<Camera3D>("Camera3D");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
