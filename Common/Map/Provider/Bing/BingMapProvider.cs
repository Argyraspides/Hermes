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



using System.Threading.Tasks;

// TODO: The map provider shouldn't be handling caching. All that logic should go into a
// BingMapTileCacher which implements the ICacheCapability<BingMapTileResource>.
// You can then instantiate a BingMapTileCacher in this class and then use it to cache stuff there.
public class BingMapProvider : IMapProvider<BingQueryParameters>
{
    private BingMapTileCacher m_bingMapTileCacher;

    public BingMapProvider()
    {
        m_bingMapTileCacher = new BingMapTileCacher();
    }

    public async Task<byte[]> RequestRawMapTileAsync(BingQueryParameters queryParameters)
    {
        // Check if resource already exists and return the cached map tile if it does
        string resourceHash = queryParameters.GetHashCode().ToString();
        if (m_bingMapTileCacher.ResourceExists(resourceHash))
        {
            MercatorMapTile mapTileResource = m_bingMapTileCacher.RetrieveResourceFromCache(resourceHash);
            return mapTileResource.ResourceData;
        }

        // Otherwise, query Bing (Microsoft is the GOAT right? I make fun of them yet here
        // I am using C# and the .NET framework)
        BingMapTileURLBuilder bingMapTileURLBuilder = new BingMapTileURLBuilder();
        string url = bingMapTileURLBuilder.BuildUrl(queryParameters);

        byte[] rawMapData = await new System.Net.Http.HttpClient().GetByteArrayAsync(url);
        return rawMapData;
    }

    public async Task<MercatorMapTile> RequestMapTileAsync(BingQueryParameters queryParameters)
    {

        // Check if resource already exists and return the cached map tile if it does
        string resourceHash = queryParameters.GetHashCode().ToString();
        if (m_bingMapTileCacher.ResourceExists(resourceHash))
        {
            MercatorMapTile mapTileResource = m_bingMapTileCacher.RetrieveResourceFromCache(resourceHash);
            return mapTileResource;
        }

        // Otherwise, query Bing (Microsoft is the GOAT right? I make fun of them yet here
        // I am using C# and the .NET framework)
        BingMapTileURLBuilder bingMapTileURLBuilder = new BingMapTileURLBuilder();
        string url = bingMapTileURLBuilder.BuildUrl(queryParameters);

        byte[] rawMapData = await new System.Net.Http.HttpClient().GetByteArrayAsync(url);

        MercatorMapTile mapTile = new MercatorMapTile(queryParameters.QuadKey, rawMapData, queryParameters.MapType);
        return mapTile;
    }
}
