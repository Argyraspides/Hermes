/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


using Godot;

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
// TODO: Each map API instance should run on its own thread. This is so that each terrain chunk, or any
// object that wishes to use the map api, can have its own instance and not have to worry about being blocked
// by other pending map api requests. This means you MAY have to change the way MapAPI and MapProvider work as
// currently they both inherit from Node, and anything inheriting from Node is meant to be a part of the main
// game thread. You could also just spawn a thread with its own process or something, idk. Think about it later
// Whatever you do, please stick to the following principles:
// - The main game should never be blocked in any way (no blocking waits)
// - Everything that *can* be asynchronous *should* be asynchronous in this context (map tile fetching & processing)
// - Robust error handling for network issues such as failed map tile requests, corrupted map data, etc
public partial class MapAPI : Node
{

    public MapProvider mapProvider;

    [Signal]
    public delegate void MapTileReceivedEventHandler(Texture2D texture2D);


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // TODO: Bing by default is okay, but this should be configurable somehow
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
        if (imageType == MapUtils.MapImageType.JPEG)
            image.LoadJpgFromBuffer(rawMapData);
        if (imageType == MapUtils.MapImageType.PNG)
            image.LoadPngFromBuffer(rawMapData);
        if (imageType == MapUtils.MapImageType.BMP)
            image.LoadBmpFromBuffer(rawMapData);

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
