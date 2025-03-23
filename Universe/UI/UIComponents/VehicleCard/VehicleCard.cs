using Godot;
using Hermes.Core.Vehicle;
using Hermes.Core.Vehicle.Components;

namespace Hermes.Universe.UI.UIComponents.VehicleCard;

public partial class VehicleCard : Control
{
    public Vehicle Vehicle { set; private get; }

    private HBoxContainer m_telemetryPanel;
    private Hermes.Universe.UI.UIComponents.CompassDisplay.CompassDisplay m_compassDisplay;

    public override void _Ready()
    {
        m_telemetryPanel = GetNode<HBoxContainer>("TelemetryPanel");

        // TODO::ARGYRASPIDES() { Move this to the telemetry panel. Parents should only reference their immediate children }
        m_compassDisplay = m_telemetryPanel.GetNode<Hermes.Universe.UI.UIComponents.CompassDisplay.CompassDisplay>("CompassDisplay");
    }

    public override void _Process(double delta)
    {
        if (Vehicle.HasComponent(ComponentType.GPS))
        {
            var gps = (GPSComponent) Vehicle.GetComponent(ComponentType.GPS);
            m_compassDisplay.HeadingDeg = gps.GPSState.Heading;
        }
    }
}