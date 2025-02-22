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

namespace Hermes.Common.Map.Types.Bing;

using Hermes.Common.Map.Utils;

public class BingMercatorMapTile : MapTile
{
    public string QuadKey { get; private set; }

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
        MapType = mapType;
        Language = language;
        MapImageType = (imageType == ImageType.UNKNOWN) ? ImageUtils.GetImageFormat(imageData) : imageType;

        QuadKey = quadKey;

        // Extract zoom level from quadkey length
        ZoomLevel = quadKey.Length;

        // Convert tile coordinates to lat/lon
        (Latitude, Longitude, ZoomLevel) = MapUtils.QuadKeyToLatLonAndZoom(quadKey);

        LatitudeTileCoo = MapUtils.LatitudeToTileCoordinateMercator(Latitude, ZoomLevel);
        LongitudeTileCoo = MapUtils.LongitudeToTileCoordinateMercator(Longitude, ZoomLevel);

        LatitudeRange = MapUtils.TileToLatRange(LatitudeTileCoo, ZoomLevel);
        LongitudeRange = MapUtils.TileToLonRange(LongitudeTileCoo);

        if (imageData != null)
        {
            (Width, Height) = ImageUtils.GetImageDimensions(imageData);
            Texture2D = ImageUtils.ByteArrayToImageTexture(imageData);
        }

        ResourceData = imageData;

        // TODO(Argyraspides, 08/02/2025) Find a way to do this automatically in the Resource class after the most derived class' constructor is
        // already finished. Do not assume a programmer will remember to GenerateHash() after deriving a class from Resource. This should
        // be completely automatic.
        GenerateHash();
        ResourcePath = Hash;
    }

    public BingMercatorMapTile()
    {
        GenerateHash();
        ResourcePath = Hash;
    }

    public override string GenerateHashCore()
    {
        var format = "Bing/{0}/{1}/{2}/{3}/tile_{4}_{5}.{6}";
        return string.Format(format,
            MapType,
            MapImageType.ToString().ToLower(),
            Language,
            ZoomLevel,
            LongitudeTileCoo,
            LatitudeTileCoo,
            MapImageType.ToString().ToLower());
    }

    public override bool IsHashable()
    {
        return MapType != MapType.UNKNOWN
               && MapImageType != ImageType.UNKNOWN
               && Language != Language.UNKNOWN
               && ZoomLevel > 0
               && LongitudeTileCoo >= 0
               && LongitudeTileCoo < int.MaxValue
               && LatitudeTileCoo >= 0
               && LatitudeTileCoo < int.MaxValue;
    }
}
