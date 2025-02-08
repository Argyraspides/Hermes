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


using System.Collections.Generic;

public class BingMapTileQueryParameters : IQueryParameters
{

    public BingMapTileQueryParameters(
        int serverInstance,
        MapType mapType,
        string quadKey,
        ImageType mapImgType,
        string apiVersion,
        Language language
    )
    {

        if (serverInstance < 0 || serverInstance >= 4)
            throw new System.ArgumentOutOfRangeException(
                nameof(serverInstance),
                "Server instance must be between 0 and 3"
            );

        if (string.IsNullOrEmpty(quadKey))
            throw new System.ArgumentException(
                "QuadKey cannot be null or empty",
                nameof(quadKey)
            );

        if (string.IsNullOrEmpty(apiVersion))
            throw new System.ArgumentException(
                "API version cannot be null or empty",
                nameof(apiVersion)
            );

        ServerInstance = serverInstance;
        MapType = mapType;
        QuadKey = quadKey;
        MapImageType = mapImgType;
        APIVersion = apiVersion;
        Language = language;
    }

    /*

    The Bing maps API query URL looks like this:
    "https://ecn.t{serverInstance}.tiles.virtualearth.net/tiles/{mapType}{quadKey}.{mapImgType}?g={apiVersion}&mkt={lang}";

    */
    public int ServerInstance { get; private set; }
    public MapType MapType { get; private set; }
    public string QuadKey { get; private set; }
    public ImageType MapImageType { get; private set; }
    public string APIVersion { get; private set; }
    public Language Language { get; private set; }

    public IDictionary<string, string> ToQueryDictionary()
    {
        var queryParams = new Dictionary<string, string>();

        queryParams[BingMapTileURLBuilder.serverInstanceStr] = ServerInstance.ToString();
        queryParams[BingMapTileURLBuilder.mapTypeStr] = MapTypeToQueryParam(MapType);
        queryParams[BingMapTileURLBuilder.quadKeyStr] = QuadKey;
        queryParams[BingMapTileURLBuilder.mapImgTypeStr] = MapImageType.ToString();
        queryParams[BingMapTileURLBuilder.apiVersionStr] = APIVersion;
        queryParams[BingMapTileURLBuilder.langStr] = Language.ToString().ToLower();

        return queryParams;
    }

    private static string MapTypeToQueryParam(MapType mapType) => mapType switch
    {
        MapType.SATELLITE => "a",
        MapType.STREET => "r",
        MapType.HYBRID => "h",
        _ => "a"
    };
}
