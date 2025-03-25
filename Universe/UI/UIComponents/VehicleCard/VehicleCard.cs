using Godot;
using Hermes.Core.Vehicle;
using Hermes.Core.Vehicle.Components;
using Hermes.Universe.UI.UIComponents.CompassDisplay;

namespace Hermes.Universe.UI.UIComponents.VehicleCard;

public partial class VehicleCard : Control
{
    public Vehicle Vehicle { set; private get; }

    private HBoxContainer m_telemetryPanel;
    private CompassDisplay.CompassDisplay m_compassDisplay;
    private RichTextLabel m_vehicleNameLabel;

    public override void _Ready()
    {
        m_telemetryPanel = GetNode<HBoxContainer>("TelemetryPanel");

        // TODO::ARGYRASPIDES() { Move this to the telemetry panel? Parents should only reference their immediate children? }
        m_compassDisplay = m_telemetryPanel.GetNode<CompassDisplay.CompassDisplay>("CompassDisplay");

        m_vehicleNameLabel =
            GetNode<HBoxContainer>("VehicleNameBox").
                GetNode<CenterContainer>("CenterContainer").
                    GetNode<RichTextLabel>("VehicleNameLabel");

        // VehicleCard's are meant to be used in the VehicleCardPanel. We're giving it a minimum size here
        // to make sure the panel resizes according to the size of this vehicle card
        CustomMinimumSize =
            new Vector2(GetViewport().GetWindow().Size.X * 0.25f,
                150);
    }

    public override void _Process(double delta)
    {
        if (Vehicle.HasComponent(ComponentType.GPS))
        {
            var gps = (GPSComponent) Vehicle.GetComponent(ComponentType.GPS);
            m_compassDisplay.HeadingDeg = gps.GPSState.Heading;
        }
        m_vehicleNameLabel.Text = Vehicle.MachineType.ToString();
    }
}
