using System.Text.RegularExpressions;
using Godot;
using Hermes.Core.Machine;
using Hermes.Core.Machine.Components;
using Hermes.Universe.UI.UIComponents.CompassDisplay;

namespace Hermes.Universe.UI.UIComponents.MachineCard;

public partial class MachineCard : Control
{
    public Machine Machine { set; get; }

    ColorRect m_colorRect;

    private HBoxContainer m_machineNameBox;
    private TextureRect m_machineTypeIcon;
    private CenterContainer m_textCenterContainer;
    private RichTextLabel m_machineNameLabel;

    private HBoxContainer m_telemetryPanel;


    private CompassDisplay.CompassDisplay m_compassDisplay;

    public override void _Ready()
    {

        m_colorRect = GetNode<ColorRect>("ColorRect");

        m_machineNameBox = GetNode<HBoxContainer>("MachineNameBox");

        m_machineTypeIcon = m_machineNameBox.GetNode<TextureRect>("MachineTypeIcon");
        m_textCenterContainer = m_machineNameBox.GetNode<CenterContainer>("TextCenterContainer");

        m_machineNameLabel = m_textCenterContainer.GetNode<RichTextLabel>("MachineNameLabel");

        m_telemetryPanel = GetNode<HBoxContainer>("TelemetryPanel");

        m_compassDisplay = m_telemetryPanel.GetNode<CompassDisplay.CompassDisplay>("CompassDisplay");

        // MachineCard's are meant to be used in the MachineCardPanel. We're giving it a minimum size here
        // to make sure the panel resizes according to the size of this machine card
        CustomMinimumSize =
            new Vector2(GetViewport().GetWindow().Size.X * 0.3f,
                225);
    }

    private void SetIcon()
    {
        if (Machine.Identity.MachineType == MachineType.Quadcopter)
        {
            m_machineTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Machine/Assets/Images/QuadcopterIcon.png");
        }
        else if (Machine.Identity.MachineType == MachineType.GroundControlStation)
        {
            m_machineTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Machine/Assets/Images/GroundControlStation.png");
        }
    }

    public override void _Process(double delta)
    {
        if (!double.IsNaN(Machine.Orientation.Heading))
        {
            m_compassDisplay.HeadingDeg = Machine.Orientation.Heading;
        }
        m_machineNameLabel.Text =
            Regex.Replace(Machine.MachineType.ToString(), @"(?<=[a-z])(?=[A-Z])|(?<=\d)(?=[A-Z])", " ");

        SetIcon();
    }


}
