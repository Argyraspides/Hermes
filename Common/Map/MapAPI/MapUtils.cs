using Godot;
using System;
using System.Text;

public static class MapUtils
{

	public enum MapType
	{
		SATELLITE,
		STREET,
		HYBRID
	}

	public enum MapImageType
	{
		PNG,
		JPG,
		BMP
	}

	// Converts line of latitude (degrees) to a latitude tile coordinate (y axis) on the Mercator projection,
	// using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
	// To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	//
	// Formula from:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
	public static int LatitudeToTileCoordinateMercator(double latitude, int zoom)
	{
		latitude = Math.Clamp(latitude, -85.05112878, 85.05112878);
		double latRad = latitude * (Math.PI / 180.0);
		return (int)Math.Floor(
			(1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom)
		);
	}

	// Converts line of longitude (degrees) to a longitude tile coordinate (y axis) on the Mercator projection,
	// using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
	// To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	//
	// Formula from:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
	public static int LongitudeToTileCoordinateMercator(double longitude, int zoom)
	{
		return (int)Math.Floor((longitude + 180.0) / 360.0 * (1 << zoom));
	}

	// Converts tile coordinates for a line of latitude (y) and line of longitude (x)
	// at a particular zoom level to a quadkey. 
	//
	// To understand quadkeys, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	// This kind of tile indexing is used mainly for the Bing maps API
	public static string TileCoordinatesToQuadkey(int x, int y, int zoom)
	{

		int maxTile = (1 << zoom) - 1;
		if (x < 0 || x > maxTile || y < 0 || y > maxTile)
		{
			throw new ArgumentException($"Tile coordinates {x},{y} are not valid for zoom level {zoom}");
		}

		StringBuilder quadkey = new StringBuilder();

		int i = zoom;
		while(i > 0)
		{
			i--;
			int digit = 0x00;
			int mask = 1 << i;

			if ((x & mask) != 0) digit += 1;
			if ((y & mask) != 0) digit += 2;
			quadkey.Append(digit.ToString());
		}

		return quadkey.ToString();
	}


}
