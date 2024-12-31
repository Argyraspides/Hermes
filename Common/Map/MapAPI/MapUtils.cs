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


using System;
using System.Text;


// This class just holds a bunch of static functions and definitions for anything map related.
// E.g., converting latitude/longitude to tile coordinates, specifying map types, constructing quadrant keys,
// image manipulation of certain map tile projections such that they can be fitted again to a 3D sphere, etc.
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
		BMP,
		JPEG,
		GIF,
		TIFF,
		PNG,
		UNKNOWN
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

	// Converts *TILE* coordinates (x,y) for a line of latitude (y) and line of longitude (x)
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
		while (i > 0)
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

	public static MapImageType GetImageFormat(byte[] imageData)
	{
		// Check if we have enough bytes to check the header
		if (imageData == null || imageData.Length < 4)
		{
			return MapImageType.UNKNOWN;
		}

		// JPEG starts with FF D8 FF
		if (imageData[0] == 0xFF && imageData[1] == 0xD8 && imageData[2] == 0xFF)
		{
			return MapImageType.JPEG;
		}

		// PNG starts with 89 50 4E 47 0D 0A 1A 0A
		if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
		{
			return MapImageType.PNG;
		}

		// GIF starts with GIF87a or GIF89a
		if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46 && imageData[3] == 0x38)
		{
			return MapImageType.GIF;
		}

		// BMP starts with BM
		if (imageData[0] == 0x42 && imageData[1] == 0x4D)
		{
			return MapImageType.BMP;
		}

		// TIFF starts with II (little endian) or MM (big endian)
		if ((imageData[0] == 0x49 && imageData[1] == 0x49) ||
			(imageData[0] == 0x4D && imageData[1] == 0x4D))
		{
			return MapImageType.TIFF;
		}

		return MapImageType.UNKNOWN;
	}


}
