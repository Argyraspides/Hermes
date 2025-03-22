using Godot;
using System;

public partial class CompassDisplay : Control
{
	// Called when the node enters the scene tree for the first time.

    private CenterContainer m_compassNeedleContainer;
    private CenterContainer m_compassLabelContainer;
    private HBoxContainer EastWest;
    private VBoxContainer NorthSouth;

    private float curr = 0.0f;

	public override void _Ready()
	{
        m_compassNeedleContainer = GetChild<CenterContainer>(0);
        m_compassLabelContainer = GetChild<CenterContainer>(1);
        EastWest = GetChild<HBoxContainer>(2);
        NorthSouth = GetChild<VBoxContainer>(3);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        curr = (curr + (10 * (float)delta)) % 360.0f;
        m_compassLabelContainer.GetChild<RichTextLabel>(0).Text = $"[center]{(int)curr}\u00b0[/center]";
        m_compassNeedleContainer.RotationDegrees = curr;
    }
}
