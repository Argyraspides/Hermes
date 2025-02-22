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


using Godot;
using System.Threading.Tasks;

public partial class MapAPI : Node
{
    private BingMapProvider m_mapProvider;

    public MapAPI()
    {
        m_mapProvider = new BingMapProvider();
    }

    // Requests a map tile at a particular latitude/longitude at a specified zoom level (degrees), with a map type
    // (e.g., satellite, street, hybrid, etc.), and an image type (PNG, JPG, etc.).
    // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
    public async Task<MapTile> RequestMapTileAsync(
        float latitude,
        float longitude,
        int zoom,
        MapType mapType,
        ImageType mapImageType
    )
    {
        BingMapTileQueryParameters bingQueryParameters = new BingMapTileQueryParameters(
            0,
            mapType,
            MapUtils.LatLonAndZoomToQuadKey(latitude, longitude, zoom),
            mapImageType,
            "523",
            Language.en
        );

        return await m_mapProvider.RequestMapTileAsync(bingQueryParameters);
    }

    public async Task<byte[]> RequestRawMapTileAsync(
        float latitude,
        float longitude,
        int zoom,
        MapType mapType,
        ImageType mapImageType
    )
    {
        BingMapTileQueryParameters bingQueryParameters = new BingMapTileQueryParameters(
            0,
            mapType,
            MapUtils.LatLonAndZoomToQuadKey(latitude, longitude, zoom),
            mapImageType,
            "523",
            Language.en
        );

        return await m_mapProvider.RequestRawMapTileAsync(bingQueryParameters);
    }
}
