
using System.Collections.Generic;

public class BingQueryParameters : IQueryParameters
{

    public BingQueryParameters(
        int serverInstance,
        MapType mapType,
        string quadKey,
        int mapImgType,
        string apiVersion,
        string language
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

        if (string.IsNullOrEmpty(language))
            throw new System.ArgumentException(
                "Language cannot be null or empty",
                nameof(language)
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
    public int ServerInstance { get; }
    public MapType MapType { get; }
    public string QuadKey { get; }
    public int MapImageType { get; }
    public string APIVersion { get; }
    public string Language { get; }

    public IDictionary<string, string> ToQueryDictionary()
    {
        var queryParams = new Dictionary<string, string>();

        queryParams[BingMapTileURLBuilder.serverInstanceStr] = ServerInstance.ToString();
        queryParams[BingMapTileURLBuilder.mapTypeStr] = MapTypeToQueryParam(MapType);
        queryParams[BingMapTileURLBuilder.quadKeyStr] = QuadKey;
        queryParams[BingMapTileURLBuilder.mapImgTypeStr] = ImageType.ToString(MapImageType);
        queryParams[BingMapTileURLBuilder.apiVersionStr] = APIVersion;
        queryParams[BingMapTileURLBuilder.langStr] = Language;

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
