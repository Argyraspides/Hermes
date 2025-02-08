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


public class BingMercatorMapTile : MapTile
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
    public BingMercatorMapTile(
        string quadKey,
        MapType mapType = MapType.UNKNOWN,
        Language language = Language.UNKNOWN,
        ImageType imageType = ImageType.UNKNOWN,
        byte[] imageData = null
    )
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
        m_size = 256;

        if (imageData != null)
        {
            m_texture2D = MapUtils.ByteArrayToImageTexture(imageData);
        }

        ResourceData = imageData;

        // TODO(Argyraspides, 08/02/2025) Find a way to do this automatically in the Resource class after the most derived class' constructor is
        // already finished. Do not assume a programmer will remember to GenerateHash() after deriving a class from Resource. This should
        // be completely automatic.
        GenerateHash();
    }

    public BingMercatorMapTile()
    {
        GenerateHash();
    }

    public override string GenerateHashCore()
    {
        var format = "Bing/{0}/{1}/{2}/{3}/tile_{4}_{5}.{6}";
        return string.Format(format,
            m_mapType,
            m_mapImageType.ToString().ToLower(),
            m_language,
            m_zoomLevel,
            m_longitudeTileCoo,
            m_latitudeTileCoo,
            m_mapImageType.ToString().ToLower());
    }

    public override bool IsHashable()
    {
        return m_mapType != MapType.UNKNOWN
            && m_mapImageType != ImageType.UNKNOWN
            && m_language != Language.UNKNOWN
            && m_zoomLevel > 0
            && m_longitudeTileCoo >= 0
            && m_longitudeTileCoo < int.MaxValue
            && m_latitudeTileCoo >= 0
            && m_latitudeTileCoo < int.MaxValue;
    }

}
