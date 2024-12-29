using Godot;
using System;
using System.Collections.Generic;

public partial class MapAPI : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MapProvider mapProvider = new BingMapProvider();
		AddChild(mapProvider, true);
		mapProvider.RawMapTileDataReceived += onRawMapTileDataReceived;
	}

	// This function is invoked by a MapProvider's "RawMapTileDataReceivedEventHandler" signal
	// when we get an HTTP response from calling an API to retrieve map tile data.
	public void onRawMapTileDataReceived(byte[] rawMapData) 
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
