using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Data;

public partial class MapProvider
{

	enum MapType {
		SATELLITE,
		STREET,
		HYBRID
	}


	// This signal is called when FetchRawMapTileData is successful and provides
	// the raw byte array for the map tile image data
	[Signal]
	public delegate void RawMapTileDataReceived(byte[] rawMapTileData);

	// Constructs the query string (URL) to obtain a map tile
	// Takes in a dictionary that maps API parameter names to their values
	public virtual string ConstructQueryString(Dictionary<string, string> dict)
	{
		return "";
	}

	// Performs an HTTP request to retrieve raw map tile data from an API provider
	// with the query string.
	public virtual Error FetchRawMapTileData(string queryString)
	{
		return Error.Ok;
	}

}
