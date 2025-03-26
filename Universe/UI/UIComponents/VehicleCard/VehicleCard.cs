using System.Text.RegularExpressions;
using Godot;
using Hermes.Core.Vehicle;
using Hermes.Core.Vehicle.Components;
using Hermes.Universe.UI.UIComponents.CompassDisplay;

namespace Hermes.Universe.UI.UIComponents.VehicleCard;

public partial class VehicleCard : Control
{
    public Vehicle Vehicle { set; get; }

    ColorRect m_colorRect;

    private HBoxContainer m_vehicleNameBox;
    private TextureRect m_vehicleTypeIcon;
    private CenterContainer m_textCenterContainer;
    private RichTextLabel m_vehicleNameLabel;

    private HBoxContainer m_telemetryPanel;


    private CompassDisplay.CompassDisplay m_compassDisplay;

    public override void _Ready()
    {

        m_colorRect = GetNode<ColorRect>("ColorRect");

        m_vehicleNameBox = GetNode<HBoxContainer>("VehicleNameBox");

        m_vehicleTypeIcon = m_vehicleNameBox.GetNode<TextureRect>("VehicleTypeIcon");
        m_textCenterContainer = m_vehicleNameBox.GetNode<CenterContainer>("TextCenterContainer");

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
        if (Vehicle.Identity.VehicleType == MachineType.Quadcopter)
        {
            m_vehicleTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Vehicle/Assets/Images/QuadcopterIcon.png");
        }
        else if (Vehicle.Identity.VehicleType == MachineType.GroundControlStation)
        {
            m_vehicleTypeIcon.Texture = GD.Load<Texture2D>("res://Core/Vehicle/Assets/Images/GroundControlStation.png");
        }
    }

    public override void _Process(double delta)
    {
        if (!double.IsNaN(Vehicle.Orientation.Heading))
        {
            m_compassDisplay.HeadingDeg = Vehicle.Orientation.Heading;
        }
        m_vehicleNameLabel.Text =
            Regex.Replace(Vehicle.MachineType.ToString(), @"(?<=[a-z])(?=[A-Z])|(?<=\d)(?=[A-Z])", " ");

        SetIcon();
    }


}
