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
    private TextureRect m_vehicleTypeIcon;
    private CenterContainer m_textCenterContainer;
    private RichTextLabel m_vehicleNameLabel;

    private HBoxContainer m_telemetryPanel;


    private CompassDisplay.CompassDisplay m_compassDisplay;

    public override void _Ready()
    {

        m_colorRect = GetNode<ColorRect>("ColorRect");

        m_machineNameBox = GetNode<HBoxContainer>("VehicleNameBox");

        m_vehicleTypeIcon = m_machineNameBox.GetNode<TextureRect>("VehicleTypeIcon");
        m_textCenterContainer = m_machineNameBox.GetNode<CenterContainer>("TextCenterContainer");

        m_vehicleNameLabel = m_textCenterContainer.GetNode<RichTextLabel>("VehicleNameLabel");

        m_telemetryPanel = GetNode<HBoxContainer>("TelemetryPanel");

        m_compassDisplay = m_telemetryPanel.GetNode<CompassDisplay.CompassDisplay>("CompassDisplay");

        // VehicleCard's are meant to be used in the VehicleCardPanel. We're giving it a minimum size here
        // to make sure the panel resizes according to the size of this vehicle card
        CustomMinimumSize =
            new Vector2(GetViewport().GetWindow().Size.X * 0.3f,
                225);
    }

    private void SetIcon()
    {
        if (Machine.Identity.MachineType == MachineType.Quadcopter)
        {
            m_vehicleTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Vehicle/Assets/Images/QuadcopterIcon.png");
        }
        else if (Machine.Identity.MachineType == MachineType.GroundControlStation)
        {
            m_vehicleTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Vehicle/Assets/Images/GroundControlStation.png");
        }
    }

    public override void _Process(double delta)
    {
        if (!double.IsNaN(Machine.Orientation.Heading))
        {
            m_compassDisplay.HeadingDeg = Machine.Orientation.Heading;
        }
        m_vehicleNameLabel.Text =
            Regex.Replace(Machine.MachineType.ToString(), @"(?<=[a-z])(?=[A-Z])|(?<=\d)(?=[A-Z])", " ");

        SetIcon();
    }


}
