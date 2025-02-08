using Godot;
using System;

public class MapTile : Resource
{
    // Tile dimensions
    public int m_width { get; protected set; }
    public int m_height { get; protected set; }

    // Geographic coordinates and ranges
    public double m_latitude { get; protected set; }
    public double m_longitude { get; protected set; }
    public int m_latitudeTileCoo { get; protected set; }
    public int m_longitudeTileCoo { get; protected set; }
    public double m_latitudeRange { get; protected set; }
    public double m_longitudeRange { get; protected set; }

    // Tile metadata
    public int m_zoomLevel { get; protected set; }
    public MapType m_mapType { get; protected set; }
    public ImageType m_mapImageType { get; protected set; }
    public Texture2D m_texture2D { get; protected set; }

    // If the map tile is a street view map tile/hybrid, the names of various places
    // will show up, hence a map tile must have a language field
    public Language m_language { get; protected set; }

    public MapTileType m_mapTileType { get; protected set; }

    public MapTile()
    {
        // Default to null island (0,0) with a small range
        m_latitude = 0.0f;
        m_longitude = 0.0f;

        // Default zoom level for city-scale viewing
        m_zoomLevel = 12;

        // Automatically determine tile coordinate, latitude/longitude range
        m_latitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(m_latitude, m_zoomLevel);
        m_longitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(m_longitude, m_zoomLevel);

        m_latitudeRange = MapUtils.TileToLatRange(m_latitudeTileCoo, m_zoomLevel);
        m_longitudeRange = MapUtils.TileToLonRange(m_zoomLevel);

        AutoDetermineFields(m_latitude, m_longitude, m_zoomLevel);
        InitializeDefaultFields();
    }

    public MapTile(float latitude, float longitude, int zoomLevel)
    {
        m_latitude = latitude;
        m_longitude = longitude;
        m_zoomLevel = zoomLevel;

        AutoDetermineFields(m_latitude, m_longitude, m_zoomLevel);
        InitializeDefaultFields();
    }

    private void InitializeDefaultFields()
    {
        // Default to standard map types
        m_mapType = MapType.SATELLITE;
        m_mapImageType = ImageType.PNG;
        m_texture2D = null;
        m_language = Language.en;
        m_mapTileType = MapTileType.WEB_MERCATOR;

        // Initialize with common web mercator tile dimensions
        m_width = 256;
        m_height = 256;
    }

    private void AutoDetermineFields(double latitude, double longitude, int zoomLevel)
    {
        // Automatically determine tile coordinate, latitude/longitude range
        m_latitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(latitude, zoomLevel);
        m_longitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(longitude, zoomLevel);
        m_latitudeRange = MapUtils.TileToLatRange(m_latitudeTileCoo, zoomLevel);
        m_longitudeRange = MapUtils.TileToLonRange(zoomLevel);
    }

    public override bool IsHashable()
    {
        throw new NotImplementedException("Resource " + this + " cannot be determined hashable. You must implement this function in any derived class of Resource");
    }

    public override string GenerateHashCore()
    {
        throw new NotImplementedException("Resource " + this + " cannot have a hash generated. You must implement this function in any derived class of Resource");
    }
}
