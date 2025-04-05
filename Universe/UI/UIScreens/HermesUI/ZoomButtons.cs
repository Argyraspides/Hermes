using Godot;
using Hermes.Universe.Autoloads.EventBus;

public partial class ZoomButtons : VBoxContainer
{

    private Button m_settingsButton;
    private Button m_zoomInButton;
    private Button m_zoomOutButton;

    public override void _Ready()
    {
        m_settingsButton = GetNode<Button>("SettingsButton");
        m_zoomInButton = GetNode<Button>("ZoomInButton");
        m_zoomOutButton = GetNode<Button>("ZoomOutButton");

        m_zoomInButton.ButtonDown += GlobalEventBus.Instance.UIEventBus.OnZoomInButtonClicked;
        m_zoomOutButton.ButtonDown += GlobalEventBus.Instance.UIEventBus.OnZoomOutButtonClicked;

    }

    public override void _Process(double delta)
	{
        Vector2I screenSize = GetTree().GetRoot().GetWindow().Size;
        GlobalPosition = new Vector2I(
            screenSize.X - 10 - (int)Size.X,
            screenSize.Y - 100 - (int)Size.Y
        );
    }
}
