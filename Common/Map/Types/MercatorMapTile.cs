using Godot;
using System;

public class MercatorMapTile : Resource
{
    // Tile dimensions
    int m_width;
    int m_height;

    // Geographic coordinates and ranges
    double m_latitude;
    double m_longitude;
    double m_latitudeRange;
    double m_longitudeRange;

    // Tile metadata
    int m_zoomLevel;
    MapType m_mapType;
    ImageType m_mapImageType;
    Texture2D m_texture2D;

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
    }

    public MercatorMapTile(string quadKey, byte[] imageData, MapType mapType)
    {
        m_mapType = mapType;
        m_mapImageType = MapUtils.GetImageFormat(imageData);

        // Extract zoom level from quadkey length
        m_zoomLevel = quadKey.Length;

        // Convert tile coordinates to lat/lon
        (m_latitude, m_longitude, m_zoomLevel) = MapUtils.QuadKeyToLatLonAndZoom(quadKey);

        int latTileCoo = MapUtils.LatitudeToTileCoordinateMercator(m_latitude, m_zoomLevel);
        int lonTileCoo = MapUtils.LongitudeToTileCoordinateMercator(m_longitude, m_zoomLevel);

        m_latitudeRange = MapUtils.TileToLatRange(latTileCoo, m_zoomLevel);
        m_longitudeRange = MapUtils.TileToLonRange(lonTileCoo);

        // Standard web mercator tile dimensions
        // TODO(Argyraspides, 06/02/2025) Change this so that the image resolution is determined from the raw byte array
        // at runtime instead of being hardcoded like this
        m_width = 256;
        m_height = 256;

        m_texture2D = MapUtils.ByteArrayToImageTexture(imageData);

        ResourceData = imageData;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            // FNV-1a 32-bit parameters:
            const int fnvOffsetBasis = unchecked((int)2166136261);
            const int fnvPrime = 16777619;
            int hash = fnvOffsetBasis;

            // // Hash the integer value for ServerInstance.
            // hash = (hash ^ ServerInstance) * fnvPrime;

            // Hash the MapType enum (cast to int).
            hash = (hash ^ (int)m_mapType) * fnvPrime;

            // Hash the QuadKey string character by character.
            string quadKey = MapUtils.LatLonAndZoomToQuadKey(m_latitude, m_longitude, m_zoomLevel);
            foreach (char c in quadKey)
            {
                hash = (hash ^ c) * fnvPrime;
            }

            // Hash the MapImageType enum (cast to int).
            hash = (hash ^ (int)m_mapImageType) * fnvPrime;

            // // Hash the APIVersion string.
            // foreach (char c in APIVersion)
            // {
            //     hash = (hash ^ c) * fnvPrime;
            // }

            // // Hash the Language string.
            // foreach (char c in Language)
            // {
            //     hash = (hash ^ c) * fnvPrime;
            // }

            return hash;
        }
    }
}
