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
using Godot;


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

	// // Converts line of latitude (degrees) to a latitude tile coordinate (y axis) on the Mercator projection,
	// // using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
	// // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	// //
	// // Formula from:
	// // https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
	// public static int LatitudeToTileCoordinateMercator(double latitude, int zoom)
	// {
	// 	latitude = Math.Clamp(latitude, -85.05112878, 85.05112878);
	// 	double latRad = latitude * (Math.PI / 180.0);
	// 	return (int)Math.Floor(
	// 		(1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom)
	// 	);
	// }

	// // Converts line of longitude (degrees) to a longitude tile coordinate (y axis) on the Mercator projection,
	// // using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
	// // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	// //
	// // Formula from:
	// // https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
	// public static int LongitudeToTileCoordinateMercator(double longitude, int zoom)
	// {
	// 	return (int)Math.Floor((longitude + 180.0) / 360.0 * (1 << zoom));
	// }

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

		float tanExpr = (float)(Math.Tan(latRad));
		float secExpr = (float)(1.0f / Math.Cos(latRad));
		float lnExpr = (float)(Math.Log(tanExpr + secExpr));

		float divisionExpr = (float)(lnExpr / Math.PI);

		float bracketExpr = 1.0f - divisionExpr;

		float finalExpr = bracketExpr * (1 << (zoom - 1));

		return (int)Math.Floor((finalExpr >= (1 << zoom)) ? (1 << zoom) - 1 : finalExpr);

	}

	// Converts line of longitude (degrees) to a longitude tile coordinate (y axis) on the Mercator projection,
	// using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
	// To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf 
	//
	// Formula from:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
	public static int LongitudeToTileCoordinateMercator(double longitude, int zoom)
	{

		int tilesPerSide = (1 << zoom);

		float numeratorExpr = (float)(longitude + 180.0f);
		float denominatorExpr = 360.0f;

		float divisionExpr = numeratorExpr / denominatorExpr;

		float finalExpr = divisionExpr * tilesPerSide;

		return (int)Math.Floor((finalExpr >= tilesPerSide) ? (tilesPerSide - 1) : finalExpr);
	}

	// Converts a map tile's x-coordinate to the corresponding line of longitude (degrees)
	// on the Mercator projection, using the Web Mercator tiling system.
	// Each successive zoom level doubles the number of tiles along both the x and y axes,
	// and tiles are indexed starting from the top-left of the map (0,0) at zoom level 0.
	// Formula from:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Tile_numbers_to_lon..2Flat._2
	// 
	// Parameters:
	// tx - Tile x-coordinate
	// zoom - Zoom level (determines the total number of tiles at this zoom)
	// 
	// Returns:
	// Longitude of the tile's western edge, in degrees.
	public static double MapTileToLongitude(int tx, int zoom)
	{
		return (double)tx / (1 << zoom) * 360.0 - 180.0;
	}

	// Converts a map tile's y-coordinate to the corresponding line of latitude (degrees)
	// on the Mercator projection, using the Web Mercator tiling system.
	// Each successive zoom level doubles the number of tiles along both the x and y axes,
	// and tiles are indexed starting from the top-left of the map (0,0) at zoom level 0.
	// Formula from:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Tile_numbers_to_lon..2Flat._2
	// 
	// Parameters:
	// ty - Tile y-coordinate
	// zoom - Zoom level (determines the total number of tiles at this zoom)
	// 
	// Returns:
	// Latitude of the tile's northern edge, in degrees.
	public static double MapTileToLatitude(int ty, int zoom)
	{
		double n = Math.PI - (2.0 * Math.PI * (double)ty / (1 << zoom));
		return (180.0 / Math.PI) * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
	}

	// Calculates the geographical bounds (latitude and longitude) of a specific map tile 
	// at a given zoom level on the Mercator projection. The bounds are defined by the 
	// minimum and maximum latitudes and longitudes that the tile covers.
	//
	// Each map tile is a rectangular region on the Earth's surface, and its size 
	// decreases as the zoom level increases (each successive zoom level doubles the 
	// number of tiles along both x and y axes).
	//
	// Parameters:
	// tx - Tile x-coordinate (longitude direction)
	// ty - Tile y-coordinate (latitude direction)
	// zoom - Zoom level (determines the total number of tiles at this zoom)
	//
	// Returns:
	// A tuple containing:
	// - latMin: Minimum latitude (southern edge of the tile), in degrees
	// - latMax: Maximum latitude (northern edge of the tile), in degrees
	// - lonMin: Minimum longitude (western edge of the tile), in degrees
	// - lonMax: Maximum longitude (eastern edge of the tile), in degrees
	//
	// Formula reference:
	// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Tile_numbers_to_lon..2Flat._2
	public static (double latMin, double latMax, double lonMin, double lonMax) GetTileLatLonBounds(int tx, int ty, int zoom)
	{
		double lonMin = MapTileToLongitude(tx, zoom);
		double lonMax = MapTileToLongitude(tx + 1, zoom);
		double latMin = MapTileToLatitude(ty + 1, zoom);
		double latMax = MapTileToLatitude(ty, zoom);
		return (latMin, latMax, lonMin, lonMax);
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

	// Given a map tile's location and zoom level, gives back the 
	// degrees of latitude that this map tile covers. Assumes the Web Mercator projection
	public static float TileToLatRange(float lat, float lon, int zoom)
	{
		// At zoom level z, each tile covers 360°/2^z degrees longitude
		float tileSizeInDegrees = 360.0f / (float)Math.Pow(2, zoom);

		// For latitude, we need to account for the Mercator projection's distortion
		// Convert lat to radians for the mercator calculation
		float latRad = lat * (float)Math.PI / 180.0f;

		// Calculate the latitude range using the inverse Mercator formula
		float latDelta = tileSizeInDegrees * (float)Math.Cos(latRad);

		return latDelta;
	}

	// Given a map tile's location and zoom level, gives back the 
	// degrees of longitude that this map tile covers. Assumes the Web Mercator projection
	public static float TileToLonRange(float lat, float lon, int zoom)
	{
		// Longitude is simpler since it's not affected by the Mercator projection's distortion
		// At zoom level z, each tile covers 360°/2^z degrees longitude
		float tileSizeInDegrees = 360.0f / (float)Math.Pow(2, zoom);

		return tileSizeInDegrees;
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

	// Converts latitude and longitude from radians to the Earth-Centered, Earth-Fixed (ECEF) 
	// coordinate system, which is a Cartesian system centered at the Earth's center of mass
	// Returns value as kilometers. Takes the Earth as a WGS84 ellipsoid
	public static Vector3 LatLonToCartesian(float lat, float lon)
	{
		// Calculate the radius of the parallel (distance from the Earth's axis of rotation)
		// at the given latitude. This accounts for the Earth's ellipsoidal shape.
		double N = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM / Math.Sqrt(1.0 - (SolarSystemConstants.EARTH_ECCENTRICITY_SQUARED *
											  Math.Pow(Math.Sin(lat), 2)));

		// X coordinate: distance from the Earth's axis (prime meridian)
		double x = N * Math.Cos(lat) * Math.Cos(lon);

		// Y coordinate: distance from the Earth's axis (90 degrees east)
		double y = N * Math.Cos(lat) * Math.Sin(lon);

		// Z coordinate: distance from the equatorial plane
		// Note: We multiply by (1-e²) to account for the polar flattening
		double z = N * (1.0 - SolarSystemConstants.EARTH_ECCENTRICITY_SQUARED) * Math.Sin(lat);

		// Convert to Godot's coordinate system
		// Godot's default: Y is up, X is right, Z is forward
		return new Vector3(
			(float)(x / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM),  // Normalize by dividing by semi-major axis
			(float)(z / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM),  // Y is up in Godot
			(float)(y / SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM)   // Swap Y and Z for Godot's coordinate system
		);
	}

}
