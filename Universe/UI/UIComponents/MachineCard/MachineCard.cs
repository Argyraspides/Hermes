namespace Hermes.Universe.UI.UIComponents.MachineCard;


using System.Text.RegularExpressions;
using Godot;
using Hermes.Core.Machine;


public partial class MachineCard : Control
{
    public Machine Machine { get; set; }

    private ColorRect m_colorRect;
    private HBoxContainer m_machineNameBox;
    private TextureRect m_machineTypeIcon;
    private RichTextLabel m_machineNameLabel;
    private HBoxContainer m_telemetryPanel;
    private GridContainer m_telemetryLabels;
    private RichTextLabel m_altitudeLabel;
    private CompassDisplay.CompassDisplay m_compassDisplay;

    public override void _Ready()
    {
        InitializeComponents();
        SetMinimumSize();
    }

    private void InitializeComponents()
    {
        m_colorRect = GetNode<ColorRect>("ColorRect");
        m_machineNameBox = GetNode<HBoxContainer>("MachineNameBox");
        m_machineTypeIcon = m_machineNameBox.GetNode<TextureRect>("MachineTypeIcon");
        var textCenterContainer = m_machineNameBox.GetNode<CenterContainer>("TextCenterContainer");
        m_machineNameLabel = textCenterContainer.GetNode<RichTextLabel>("MachineNameLabel");
        m_telemetryPanel = GetNode<HBoxContainer>("TelemetryPanel");
        m_compassDisplay = m_telemetryPanel.GetNode<CompassDisplay.CompassDisplay>("CompassDisplay");
        m_telemetryLabels = m_telemetryPanel.GetNode<GridContainer>("TelemetryLabels");
        m_altitudeLabel = m_telemetryLabels.GetNode<RichTextLabel>("AltitudeLabel");
    }

    private void SetMinimumSize()
    {
        // MachineCard's are meant to be used in the MachineCardPanel
        // Set minimum size to ensure the panel resizes accordingly
        CustomMinimumSize = new Vector2(GetViewport().GetWindow().Size.X * 0.3f, 225);
    }

    private void UpdateMachineTypeIcon()
    {
        string iconPath = Machine.Identity.MachineType switch
        {
            MachineType.Quadcopter => "res://Core/Machine/Assets/Images/QuadcopterIcon.png",
            MachineType.GroundControlStation => "res://Core/Machine/Assets/Images/GroundControlStation.png",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(iconPath)) return;
        m_machineTypeIcon.Texture = GD.Load<Texture2D>(iconPath);
    }

    private void UpdateMachineName()
    {
        m_machineNameLabel.Text = Regex.Replace(
            Machine.MachineType.ToString(),
            @"(?<=[a-z])(?=[A-Z])|(?<=\d)(?=[A-Z])",
            " "
        );
    }

    private void UpdateAltitude()
    {
        if (double.IsNaN(Machine._Position.Altitude)) return;
        m_altitudeLabel.Text = "\tALT:\t\t\t\t" + Machine._Position.Altitude.ToString("F1") + " m";
    }

    private void UpdateCompass()
    {
        if (double.IsNaN(Machine.Orientation.Heading)) return;
        m_compassDisplay.HeadingDeg = Machine.Orientation.Heading;
    }

    public override void _Process(double delta)
    {
        UpdateCompass();
        UpdateMachineName();
        UpdateMachineTypeIcon();
        UpdateAltitude();
    }
}
