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

using Hermes.Common.Map.Utils;
using Hermes.Common.Networking.Cache;
using Hermes.Common.Types;

namespace Hermes.Common.Map.Types;

using Godot;
using System;

public class MapTile : HermesResource
{
    // Tile dimensions
    public int Width { get; protected set; } = 256;
    public int Height { get; protected set; } = 256;

    // Geographic coordinates and ranges
    public double Latitude { get; protected set; }
    public double Longitude { get; protected set; }
    public int LatitudeTileCoo { get; protected set; }
    public int LongitudeTileCoo { get; protected set; }
    public double LatitudeRange { get; protected set; }
    public double LongitudeRange { get; protected set; }

    // Tile metadata
    public int ZoomLevel { get; protected set; } = 12;
    public MapType MapType { get; protected set; } = MapType.SATELLITE;
    public ImageType MapImageType { get; protected set; } = ImageType.PNG;
    public Texture2D Texture2D { get; protected set; } = null;

    // If the map tile is a street view map tile/hybrid, the names of various places
    // will show up, hence a map tile must have a language field
    public HumanLanguage Language { get; protected set; } = HumanLanguage.en;

    public MapTileType MapTileType { get; protected set; } = MapTileType.WEB_MERCATOR_WGS84;

    public MapTile()
    {
        // Default to null island (0,0) with a small range
        Latitude = 0.0f;
        Longitude = 0.0f;

        // Default zoom level for city-scale viewing
        ZoomLevel = 12;

        // Automatically determine tile coordinate, latitude/longitude range
        LatitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(Latitude, ZoomLevel);
        LongitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(Longitude, ZoomLevel);

        LatitudeRange = MapUtils.TileToLatRange(LatitudeTileCoo, ZoomLevel);
        LongitudeRange = MapUtils.TileToLonRange(ZoomLevel);

        AutoDetermineFields(Latitude, Longitude, ZoomLevel);
    }

    public MapTile(double latitude, double longitude, int zoomLevel, MapTileType tileType = MapTileType.WEB_MERCATOR_WGS84)
    {
        Latitude = latitude;
        Longitude = longitude;
        ZoomLevel = zoomLevel;

        AutoDetermineFields(Latitude, Longitude, ZoomLevel);
    }

    private void AutoDetermineFields(double latitude, double longitude, int zoomLevel)
    {
        // Automatically determine tile coordinate, latitude/longitude range
        LatitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(latitude, zoomLevel);
        LongitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(longitude, zoomLevel);
        LatitudeRange = MapUtils.TileToLatRange(LatitudeTileCoo, zoomLevel);
        LongitudeRange = MapUtils.TileToLonRange(zoomLevel);
    }

    public override bool IsHashable()
    {
        throw new NotImplementedException("Resource " + this +
                                          " cannot be determined hashable. You must implement this function in any derived class of Resource");
    }

    public override string GenerateHashCore()
    {
        throw new NotImplementedException("Resource " + this +
                                          " cannot have a hash generated. You must implement this function in any derived class of Resource");
    }
}
