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


/// <summary>
/// Builds a URL for querying map tiles from the Bing backend API. This is the single source of truth for
/// URL parameters
///
/// See: https://learn.microsoft.com/en-us/bingmaps/rest-services/directly-accessing-the-bing-maps-tiles
///
/// TODO::WARNING(Argyraspides, 06/02/2025) The URL that this class uses will be deprecated in June. Fix it up before then!
///
/// </summary>
public class BingMapTileURLBuilder : IUrlBuilder<BingMapTileQueryParameters>
{
    public const string serverInstanceStr = "serverInstance";
    public const string mapTypeStr = "mapType";
    public const string quadKeyStr = "quadKey";
    public const string mapImgTypeStr = "mapImgType";
    public const string apiVersionStr = "apiVersion";
    public const string langStr = "lang";

    public readonly string URLTemplate =
        $"https://ecn.t{{{serverInstanceStr}}}.tiles.virtualearth.net/tiles/{{{mapTypeStr}}}{{{quadKeyStr}}}.{{{mapImgTypeStr}}}?g={{{apiVersionStr}}}&mkt={{{langStr}}}";

    public string BuildUrl(BingMapTileQueryParameters parameters)
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
