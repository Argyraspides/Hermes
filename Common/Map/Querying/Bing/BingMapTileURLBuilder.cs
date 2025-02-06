
/// <summary>
/// Builds a URL for querying map tiles from the Bing backend API. This is the single source of truth for
/// URL parameters
///
/// See: https://learn.microsoft.com/en-us/bingmaps/rest-services/directly-accessing-the-bing-maps-tiles
///
/// TODO::WARNING(Argyraspides, 06/02/2025) The URL that this class uses will be deprecated in June. Fix it up before then!
///
/// </summary>
public class BingMapTileURLBuilder : IUrlBuilder<BingQueryParameters>
{

    public const string serverInstanceStr = "serverInstance";
    public const string mapTypeStr = "mapType";
    public const string quadKeyStr = "quadKey";
    public const string mapImgTypeStr = "mapImgType";
    public const string apiVersionStr = "apiVersion";
    public const string langStr = "lang";

    public string URLTemplate =
    $"https://ecn.t{serverInstanceStr}.tiles.virtualearth.net/tiles/{mapTypeStr}{quadKeyStr}.{mapImgTypeStr}?g={apiVersionStr}&mkt={langStr}";

    public string BuildUrl(BingQueryParameters parameters)
    {
        var parameterKvp = parameters.ToQueryDictionary();
        string finalURL = URLTemplate;

        foreach (var kvp in parameterKvp)
        {
            finalURL = finalURL.Replace("{" + kvp.Key + "}", kvp.Value);
        }
        return finalURL;
    }

}
