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

using Hermes.Common.Map.Caching.Bing;
using Hermes.Common.Map.Querying.Bing;
using Hermes.Common.Map.Types;

namespace Hermes.Common.Map.Provider.Bing;

using System.Threading.Tasks;
using Hermes.Common.Map.Types.Bing;

public class BingMapProvider : IMapProvider<BingMapTileQueryParameters>
{
    private BingMapTileCacher m_bingMapTileCacher;

    public BingMapProvider()
    {
        m_bingMapTileCacher = new BingMapTileCacher();
    }

    public async Task<byte[]> RequestRawMapTileAsync(BingMapTileQueryParameters queryParameters)
    {
        throw new System.NotImplementedException();
    }

    public async Task<MapTile> RequestMapTileAsync(BingMapTileQueryParameters queryParameters)
    {
        // Check if resource already exists and return the cached map tile if it does
        // This partialTile contains enough information to uniquely identify a map tile in the cache
        BingMercatorMapTile partialTile = new BingMercatorMapTile(
            queryParameters.QuadKey,
            queryParameters.MapType,
            queryParameters.Language,
            queryParameters.MapImageType,
            null
        );

        if (m_bingMapTileCacher.ResourceExists(partialTile))
        {
            BingMercatorMapTile mapTileResource = m_bingMapTileCacher.RetrieveResourceFromCache(partialTile);
            return mapTileResource;
        }

        // Otherwise, query Bing (Microsoft is the GOAT right? I make fun of them yet here
        // I am using C# and the .NET framework)
        BingMapTileURLBuilder bingMapTileURLBuilder = new BingMapTileURLBuilder();
        string url = bingMapTileURLBuilder.BuildUrl(queryParameters);

        byte[] rawMapData = await new System.Net.Http.HttpClient().GetByteArrayAsync(url);

        BingMercatorMapTile bingMercatorMapTile = new BingMercatorMapTile(
            queryParameters.QuadKey,
            queryParameters.MapType,
            queryParameters.Language,
            queryParameters.MapImageType,
            rawMapData
        );

        m_bingMapTileCacher.CacheResource(bingMercatorMapTile);

        return bingMercatorMapTile;
    }
}
