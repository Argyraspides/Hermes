using Godot;

namespace Hermes.Universe.UI.UIComponents.CompassDisplay;

public partial class CompassDisplay : Control
{
    // Called when the node enters the scene tree for the first time.

    private CenterContainer m_compassNeedleContainer;
    private CenterContainer m_compassLabelContainer;
    private HBoxContainer EastWest;
    private VBoxContainer NorthSouth;

    public double HeadingDeg = 0.0f;

    public override void _Ready()
    {
        m_compassNeedleContainer = GetChild<CenterContainer>(0);
        m_compassLabelContainer = GetChild<CenterContainer>(1);
        EastWest = GetChild<HBoxContainer>(2);
        NorthSouth = GetChild<VBoxContainer>(3);

        // Make sure the compass needle rotates around the triangle center
        m_compassNeedleContainer.PivotOffset = m_compassNeedleContainer.Size / 2;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        m_compassLabelContainer.GetChild<RichTextLabel>(0).Text = $"[center] {(int)HeadingDeg}\u00b0[/center]";
            m_compassNeedleContainer.RotationDegrees = (float)HeadingDeg;
    }
}
