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

    private MapProvider m_mapProvider;

    // Requests a map tile at a particular latitude/longitude at a specified zoom level (degrees)
    // To understand map tiling, see: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf

    public MapAPI()
    {
        // TODO(Argyraspides, 02/02/2025): Do not hardcode map provider. Make this configurable somehow
        // through maybe injection idk
        m_mapProvider = new BingMapProvider();
    }

    public async Task<byte[]> RequestMapTileAsync(
        float latitude,
        float longitude,
        int zoom,
        MapType mapType,
        MapUtils.MapImageType mapImageType
    )
    {

        MapProvider.QueryParameters queryParameters = m_mapProvider.ConstructQueryParameters(
            latitude,
            longitude,
            zoom,
            mapType,
            mapImageType
        );

        string queryString = m_mapProvider.ConstructQueryString(queryParameters);
        byte[] tileData = await TileFetcher.FetchTileDataAsync(queryString);
        return tileData;
    }


}
