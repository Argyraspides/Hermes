
using System.Collections.Generic;

public class BingQueryParameters : IQueryParameters
{

    public BingQueryParameters(
        int serverInstance,
        MapType mapType,
        string quadKey,
        ImageType mapImgType,
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
    public ImageType MapImageType { get; }
    public string APIVersion { get; }
    public string Language { get; }

    public IDictionary<string, string> ToQueryDictionary()
    {
        var queryParams = new Dictionary<string, string>();

        queryParams[BingMapTileURLBuilder.serverInstanceStr] = ServerInstance.ToString();
        queryParams[BingMapTileURLBuilder.mapTypeStr] = MapTypeToQueryParam(MapType);
        queryParams[BingMapTileURLBuilder.quadKeyStr] = QuadKey;
        queryParams[BingMapTileURLBuilder.mapImgTypeStr] = MapImageType.ToString();
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

    /// <summary>
    /// The BingQueryParameters object has its own hash implementation so that we can use
    /// this as a means of generating a unique hash to cache map tiles as they are requested.
    ///
    /// We make sure to use an implemenation that doesn't rely on C#'s GetHashCode() for constituent
    /// elements so that this can be replicated in any language should we ever wish to auto-generate
    /// resource hash's for custom map tiles
    /// </summary>
    /// <returns>A unique hash code that represents the specific map tile retrieved by this query string</returns>
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
            hash = (hash ^ (int)MapType) * fnvPrime;

            // Hash the QuadKey string character by character.
            foreach (char c in QuadKey)
            {
                hash = (hash ^ c) * fnvPrime;
            }

            // Hash the MapImageType enum (cast to int).
            hash = (hash ^ (int)MapImageType) * fnvPrime;

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
