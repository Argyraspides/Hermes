using Godot;
using System;
using System.Collections.Generic;
using Hermes.Core.Machine;
using Hermes.Languages.HellenicGateway.CommandDispatchers.MAVLink;
using Hermes.Universe.Autoloads.EventBus;

public partial class ControlPanel : PanelContainer
{

    Dictionary<uint, Machine> m_machines = new Dictionary<uint, Machine>();

    MAVLinkCommandFactory m_commandFactory = new MAVLinkCommandFactory();

    private ColorRect m_leftSpacer;
    private ColorRect m_rightSpacer;

    private TextureButton m_takeoffButton;
    private VSlider m_takeoffAltitudeSlider;
    private HSlider m_confirmationSlider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {

        m_takeoffButton =
            GetNode<TextureButton>
                ("VBoxContainer/ControlPanelControls/MarginContainer/TakeoffControlComponent/TakeoffButton/TextureButton");

        //m_takeoffAltitudeSlider =
         //   GetNode<VSlider>
         //   ("ControlPanel/VBoxContainer/ControlPanelControls/MarginContainer/TakeoffControlComponent/AltitudeSliderComponent/SliderCenterContainer/VSlider");

       // m_confirmationSlider =
       //     GetNode<HSlider>
       //         ("ControlAndTelemetryPanels/ControlPanel/VBoxContainer/VBoxContainer/ConfirmationSliderMargin/ConfirmationSlider/HSlider");


        //m_takeoffAltitudeSlider.MinValue = 0;
        //m_takeoffAltitudeSlider.MaxValue = 15;

        GlobalEventBus.Instance.UIEventBus.MachineCardClicked += OnMachineCardClicked;
        m_takeoffButton.Pressed += OnTakeoffButtonPressed;
        //m_confirmationSlider.DragEnded += OnConfirmationSliderSlided;

    }

	public override void _Process(double delta)
	{
	}

    private void OnTakeoffButtonPressed()
    {
        foreach (var machine in m_machines)
        {
            if (machine.Value.MachineType == MachineType.Quadcopter)
            {
                m_commandFactory.TakeoffQuadcopter(machine.Value, 15);
            }
        }
    }

    private void OnMachineCardClicked(Machine machine)
    {

        /*
         * TODO::ARGYRASPIDES() {
         *      Ideally, here we should do something like have a LINQ query or something that goes
         *      through some list defined elsewhere that tells us the functionality of each vehicle type.
         *      For example, if we select two quadcopters, then the only list we will have to go through is the
         *      "quadcopter capability list" or something, and we can show all UI elements for that
         *      If we select a quadcopter and a boat, we should go through both lists, find out which
         *      functions are common (can easily be done with LINQ), and then get the UI elements for that.
         *      This means there should be:
         *          - List of all capabilities (perhaps an enum called "Capabilities")
         *          - List of all capabilities per vehicle type (each vehicle enum capability will reference the "Capabilities")
         *          - List of UI components corresponding to each capability
         *      For now we hardcode takeoff for testing purposes.
         *  }
         */

        if (!machine.MachineId.HasValue)
        {
            return;
        }

        m_machines[machine.MachineId.Value] = machine;

    }

    private void OnConfirmationSliderSlided(bool idkWhatThisParameterIsFor)
    {
        if (m_confirmationSlider.Value == m_confirmationSlider.MaxValue)
        {

        }
    }

}
