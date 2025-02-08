using Godot;
using System;

public class MercatorMapTile : Resource
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

    public MercatorMapTile()
    {
        // Initialize with common web mercator tile dimensions
        m_width = 256;
        m_height = 256;

        // Default to null island (0,0) with a small range
        m_latitude = 0.0f;
        m_longitude = 0.0f;
        m_latitudeRange = 0.1f;
        m_longitudeRange = 0.1f;

        // Default zoom level for city-scale viewing
        m_zoomLevel = 12;

        // Default to standard map types
        m_mapType = MapType.SATELLITE;
        m_mapImageType = ImageType.PNG;
        m_texture2D = null;
        m_language = Language.en;
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
