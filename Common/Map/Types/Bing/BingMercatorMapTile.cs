public class BingMercatorMapTile : MercatorMapTile
{

    public string m_quadKey { get; private set; }

    /// <summary>
    /// This constructor can be used if one wants to provide the minimum amount of information necessary to uniquely identify a tile
    /// </summary>
    /// <param name="quadKey"></param>
    /// <param name="imageData"></param>
    /// <param name="mapType"></param>
    /// <param name="imageType"></param>
    /// <param name="language"></param>
    public BingMercatorMapTile(string quadKey, MapType mapType, Language language, ImageType imageType = ImageType.UNKNOWN, byte[] imageData = null)
    {
        m_mapType = mapType;
        m_language = language;
        m_mapImageType = (imageType == ImageType.UNKNOWN) ? MapUtils.GetImageFormat(imageData) : imageType;

        m_quadKey = quadKey;

        // Extract zoom level from quadkey length
        m_zoomLevel = quadKey.Length;

        // Convert tile coordinates to lat/lon
        (m_latitude, m_longitude, m_zoomLevel) = MapUtils.QuadKeyToLatLonAndZoom(quadKey);

        m_latitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(m_latitude, m_zoomLevel);
        m_longitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(m_longitude, m_zoomLevel);

        m_latitudeRange = MapUtils.TileToLatRange(m_latitudeTileCoo, m_zoomLevel);
        m_longitudeRange = MapUtils.TileToLonRange(m_longitudeTileCoo);

        // Standard web mercator tile dimensions
        // TODO(Argyraspides, 06/02/2025) Change this so that the image resolution is determined from the raw byte array
        // at runtime instead of being hardcoded like this
        m_width = 256;
        m_height = 256;

        m_texture2D = MapUtils.ByteArrayToImageTexture(imageData);

        ResourceData = imageData;

        GenerateHash();
    }

    public BingMercatorMapTile()
    {

    }

    public override void GenerateHash()
    {
        // Can be used as a folder path. E.g.,
        // ID = Bing/SATELLITE/PNG/en/6/tile_5_24.png
        var format = "Bing/{0}/{1}/{2}/{3}/tile_{4}_{5}.{6}";
        Hash = string.Format(format,
            m_mapType,
            m_mapImageType.ToString().ToLower(),
            m_language,
            m_zoomLevel,
            m_longitudeTileCoo,
            m_latitudeTileCoo,
            m_mapImageType.ToString().ToLower());
    }

}
