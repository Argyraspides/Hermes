using Godot;

namespace Hermes.Universe.UI.UIComponents.CompassDisplay;

public partial class CompassDisplay : Control
{
    public double? HeadingDeg = 0.0f;

    private CenterContainer m_compassNeedleContainer;
    private CenterContainer m_compassLabelContainer;
    private HBoxContainer m_eastWest;
    private VBoxContainer m_northSouth;

    private RichTextLabel m_compassHeadingLabel;

    public override void _Ready()
    {

        m_compassNeedleContainer = GetNode<CenterContainer>("CompassNeedleContainer");
        m_compassLabelContainer = GetNode<CenterContainer>("CompassLabelContainer");
        m_eastWest = GetNode<HBoxContainer>("EastWest");
        m_northSouth = GetNode<VBoxContainer>("NorthSouth");

        m_compassHeadingLabel = m_compassLabelContainer.GetNode<RichTextLabel>("CompassHeadingLabel");

        // Make sure the compass needle rotates around the triangle center
        m_compassNeedleContainer.PivotOffset = m_compassNeedleContainer.Size / 2;
    }

    public override void _Process(double delta)
    {
        string headingText = HeadingDeg.HasValue ? HeadingDeg.Value.ToString("F0") : "N/A";

        m_compassHeadingLabel.Text = $"[center] {headingText}\u00b0[/center]";
        m_compassNeedleContainer.RotationDegrees = HeadingDeg.HasValue ? (float) HeadingDeg.Value : 0.0f;
    }
}
