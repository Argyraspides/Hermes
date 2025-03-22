using Godot;
using System;

public partial class CompassDisplay : TextureRect
{
    public float CompassAngle = 90.0f;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator();
        CompassAngle = randomNumberGenerator.Randf() * 90.0f;
        RotationDegrees = CompassAngle;
    }
}
