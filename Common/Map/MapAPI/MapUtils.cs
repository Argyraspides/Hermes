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
using System.Reflection.Metadata;
using System.Text;
using Godot;


// This class just holds a bunch of static functions and definitions for anything map related.
// E.g., converting latitude/longitude to tile coordinates, specifying map types, constructing quadrant keys,
// image manipulation of certain map tile projections such that they can be fitted again to a 3D sphere, etc.
// We speak the language of Mathematics, and as such, all angles are in radians unless it is impossible
// to work with
public static class MapUtils
{


    public const double PI = Math.PI;

    // These aren't necessarily a universal constant. You can stop the Web Mercator projection at
    // any latitude you want besides the poles themselves which stretch to infinity. These
    // are just constants that most map providers use
    public const double MIN_LATITUDE_LEVEL_WEB_MERCATOR = -1.484422229745;
    public const double MAX_LATITUDE_LEVEL_WEB_MERCATOR = 1.484422229745;

    public const double RADIANS_TO_DEGREES = 180.0 / PI;
    public const double DEGREES_TO_RADIANS = PI / 180.0;

    public const double TWO_PI = PI * 2.0;


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

    // Converts line of latitude (radians) to a latitude tile coordinate (y axis) on the Mercator projection,
    // using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
    // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
    //
    // Formula from:
    // https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
    public static int LatitudeToTileCoordinateMercator(double lat, int zoom)
    {
        lat = Math.Clamp(
            lat,
            MIN_LATITUDE_LEVEL_WEB_MERCATOR,
            MAX_LATITUDE_LEVEL_WEB_MERCATOR
        );

        double tanExpr = Math.Tan(lat);
        double secExpr = 1.0 / Math.Cos(lat);
        double lnExpr = Math.Log(tanExpr + secExpr);

        double divisionExpr = lnExpr / PI;

        double bracketExpr = 1.0 - divisionExpr;

        double finalExpr = bracketExpr * (1 << (zoom - 1));

        return (int)Math.Floor((finalExpr >= (1 << zoom)) ? (1 << zoom) - 1 : finalExpr);

    }

    // Converts line of longitude (radians) to a longitude tile coordinate (y axis) on the Mercator projection,
    // using a quadtree to represent the map (each successive zoom level doubles the tiles on the X and Y axis)
    // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
    //
    // Formula from:
    // https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
    public static int LongitudeToTileCoordinateMercator(double lon, int zoom)
    {

        double lonDeg = lon * RADIANS_TO_DEGREES;

        int tilesPerSide = 1 << zoom;

        double numeratorExpr = lonDeg + 180.0;
        double denominatorExpr = 360.0;

        double divisionExpr = numeratorExpr / denominatorExpr;

        double finalExpr = divisionExpr * tilesPerSide;

        return (int)Math.Floor((finalExpr >= tilesPerSide) ? (tilesPerSide - 1) : finalExpr);
    }

    // Converts a map tile's x-coordinate to the corresponding line of longitude (radians)
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
    // Longitude of the tile's western edge, in radians.
    public static double MapTileToLongitude(int tx, int zoom)
    {
        return (double)tx / (1 << zoom) * TWO_PI - PI;
    }

    // Converts a map tile's y-coordinate to the corresponding line of latitude (radians)
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
    // Latitude of the tile's northern edge, in radians.
    public static double MapTileToLatitude(int ty, int zoom)
    {
        double n = PI - (2.0 * PI * (double)ty / (1 << zoom));
        return Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
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
    // - latMin: Minimum latitude (southern edge of the tile), in radians
    // - latMax: Maximum latitude (northern edge of the tile), in radians
    // - lonMin: Minimum longitude (western edge of the tile), in radians
    // - lonMax: Maximum longitude (eastern edge of the tile), in radians
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

            if ((x & mask) != 0)
                digit += 1;
            if ((y & mask) != 0)
                digit += 2;
            quadkey.Append(digit.ToString());
        }

        return quadkey.ToString();
    }

    // Given a map tile's location and zoom level, gives back the
    // degrees of latitude that this map tile covers. Assumes the Web Mercator projection
    // TODO: UNTESTED FUNCTION
    public static double TileToLatRange(double lat, int zoom)
    {
        // At zoom level z, each tile covers 360°/2^z degrees longitude
        double tileSizeInRadians = TWO_PI / (1 << zoom);
        // For latitude, we need to account for the Mercator projection's distortion
        // Convert lat to radians for the mercator calculation

        // Calculate the latitude range using the inverse Mercator formula
        double latDelta = tileSizeInRadians * Math.Cos(lat);

        return latDelta;
    }

    // Given a map tile's location and zoom level, gives back the
    // degrees of longitude that this map tile covers. Assumes the Web Mercator projection
    // TODO: UNTESTED FUNCTION
    public static double TileToLonRange(double lat, double lon, int zoom)
    {
        // Longitude is simpler since it's not affected by the Mercator projection's distortion
        // At zoom level z, each tile covers 360°/2^z degrees longitude (or 2*Pi / 2^z)
        return TWO_PI / (1 << zoom);
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
    // Returns value as kilometers. Takes the Earth as a WGS84 ellipsoid.
    public static Vector3 LatLonToCartesian(double lat, double lon)
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

    // Converts a raw byte array to an image texture.
    // Detects whether the image is a JPEG, PNG, or BMP, and returns the
    // appropriate ImageTexture
    public static ImageTexture ByteArrayToImageTexture(byte[] rawMapData)
    {

        MapImageType imageType = GetImageFormat(rawMapData);

        Image image = new Image();

        if (imageType == MapImageType.JPEG)
        {
            image.LoadJpgFromBuffer(rawMapData);
        }
        if (imageType == MapImageType.PNG)
        {
            image.LoadPngFromBuffer(rawMapData);
        }
        if (imageType == MapImageType.BMP)
        {
            image.LoadBmpFromBuffer(rawMapData);
        }

        ImageTexture texture = new ImageTexture();
        texture.SetImage(image);
        return texture;
    }

}
