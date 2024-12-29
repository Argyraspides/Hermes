using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using static MapUtils;

public partial class MapAPI : Node
{

	public MapProvider mapProvider;

	[Signal]
	public delegate void MapTileReceivedEventHandler(Texture2D texture2D);


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mapProvider = new BingMapProvider();
		AddChild(mapProvider, true);
		mapProvider.RawMapTileDataReceived += onRawMapTileDataReceived;
	}

	// This function is invoked by a MapProvider's "RawMapTileDataReceivedEventHandler" signal
	// when we get an HTTP response from calling an API to retrieve map tile data.
	public void onRawMapTileDataReceived(byte[] rawMapData)
	{
		Image image = new Image();
		image.LoadJpgFromBuffer(rawMapData);
		ImageTexture texture = new ImageTexture();
		texture.SetImage(image);

		Sprite2D sprite = new Sprite2D();
		sprite.Texture = texture;

		AddChild(sprite);
		EmitSignal("MapTileReceived", texture);
	}


	// Requests a map tile at a particular latitude/longitude at a specified zoom level (degrees)
	// To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
	// If successful, the function onRawMapTileDataReceived function will automatically be invoked
	public void RequestMapTile(float latitude, float longitude, int zoom)
	{
		mapProvider.RequestMapTile(latitude, longitude, zoom);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
