
using System.Collections.Generic;
using System.Collections.Specialized;

public class BingQueryParameters : IQueryParameters
{

    public BingQueryParameters(
        int serverInstance,
        MapType mapType,
        string quadKey,
        int mapImgType,
        string apiVersion,
        string lang
    )
    {
        ServerInstance = serverInstance;
        MapType = mapType;
        QuadKey = quadKey;
        MapImageType = mapImgType;
        APIVersion = apiVersion;
        Language = lang;
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

        queryParams["serverInstance"] = ServerInstance.ToString();
        queryParams["mapType"] = ImageType.ToString(MapImageType);


        return queryParams;
    }
}
