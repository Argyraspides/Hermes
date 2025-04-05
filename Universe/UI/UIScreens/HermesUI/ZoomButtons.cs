using Godot;
public partial class ZoomButtons : VBoxContainer
{
	public override void _Process(double delta)
	{
        Vector2I screenSize = GetTree().GetRoot().GetWindow().Size;
        GlobalPosition = new Vector2I(
            screenSize.X - 10 - (int)Size.X,
            screenSize.Y - 100 - (int)Size.Y
        );
    }
}
