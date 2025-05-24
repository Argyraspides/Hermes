using Godot;
using System;
using Hermes.Core.Autoloads.EventBus;


// TOOD:: this doesn't keep track of state -- where should we keep track of state?? State manager!!! figure it out BRO!!!
public partial class UserSelectedAction : RichTextLabel
{
	public override void _Ready()
    {
        Text = "";

        GlobalEventBus.Instance.UIEventBus.TakeoffControlClicked += OnTakeoffControlClicked;
        GlobalEventBus.Instance.UIEventBus.LandControlClicked += OnLandControlClicked;
    }

    private void OnTakeoffControlClicked(bool clickedState)
    {
        if (clickedState)
        {
            Text = "     [color=green]TAKEOFF SELECTED - USE SLIDER TO CONFIRM";
        }
        else
        {
            Text = "";
        }
    }

    private void OnLandControlClicked(bool clickedState)
    {
        if (clickedState)
        {
            Text = "     [color=green]LAND SELECTED - USE SLIDER TO CONFIRM";
        }
        else
        {
            Text = "";
        }
    }

	public override void _Process(double delta)
	{
	}
}
