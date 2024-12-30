using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using static MapUtils;

/** 

	An in-program API to use the map providers. 
	Example usage (C#):

	
	MapAPI mapApi = new MapAPI();

	// Connect the mapApi's signal that a map tile has been received to your own custom handler function.
	// The handler function must have an argument of "Texture2D" which will be the actual map tile image 
	// you get
	mapApi.MapTileReceived += myMapTileReceivedHandlerFunction

	// You should attach the MapAPI to a node in your scene tree. There is no constructor,
	// and setup relies on the MapAPI's _Ready() function
	AddChild(mapApi);

	// Request a map tile. Once a map tile has been successfully fetched, your handler
	// function will automatically be called
	mapApi.RequestMapTile(-36.85f, 174.76f, 5);


*/ 
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

		MapUtils.MapImageType imageType = MapUtils.GetImageFormat(rawMapData);
		
		Image image = new Image();
		if(imageType == MapUtils.MapImageType.JPEG) image.LoadJpgFromBuffer(rawMapData);
		if(imageType == MapUtils.MapImageType.PNG) image.LoadPngFromBuffer(rawMapData);
		if(imageType == MapUtils.MapImageType.BMP) image.LoadBmpFromBuffer(rawMapData);

		ImageTexture texture = new ImageTexture();
		texture.SetImage(image);

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
