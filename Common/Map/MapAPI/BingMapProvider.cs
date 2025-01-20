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
using System;
using System.Collections.Generic;
using System.Text;
using static MapUtils;

// TODO: Add documentation on what this is
public partial class BingMapProvider : MapProvider
{

    const string m_QUERY_STR_TEMPLATE =
        "https://ecn.t{server}.tiles.virtualearth.net/tiles/"
        + "{mapType}{quadKey}.{mapTypeImageFormat}"
        + "?g={apiVersion}&mkt={lang}";

    int m_nextServerInstance = 0;


    public BingMapProvider() : base()
    {
        m_mapType = MapUtils.MapType.SATELLITE;
        m_mapImageType = MapUtils.MapImageType.JPEG;
    }

    // Constructs a query string for obtaining a map tile from Bing's map provider API.
    // Takes in a dictionary which should contain mappings from the name of the
    // parameter name (string) to the value of the parameter (string)
    // Dictionary keys must include:
    //
    // "mapType" 				("a" for satellite, "r" for street view, "h" for hybrid)
    // "quadKey" 				(quadrant key value)
    // "mapTypeImageFormat"		(image file type, JPG for satellite & hybrid views, PNG for street)
    // "apiVersion"				(API version number)
    // "lang"					(language, e.g., "en" for English)
    public override string ConstructQueryString(Dictionary<string, string> dict)
    {

        StringBuilder finalQueryString = new StringBuilder(m_QUERY_STR_TEMPLATE);
        finalQueryString.Replace("{server}", NextServerNumber().ToString());

        foreach (KeyValuePair<string, string> apiParamPair in dict)
        {
            string replacementStr = "{" + apiParamPair.Key + "}";
            finalQueryString.Replace(replacementStr, apiParamPair.Value);
        }

        return finalQueryString.ToString();
    }

    public Dictionary<string, string> ConstructQueryParams(string quadkey)
    {
        Dictionary<string, string> apiQueryParams = new Dictionary<string, string>();

        if (m_mapType == MapType.SATELLITE)
        {
            apiQueryParams.Add("mapType", "a");
        }
        else if (m_mapType == MapType.STREET)
        {
            apiQueryParams.Add("mapType", "r");
        }
        else if (m_mapType == MapType.HYBRID)
        {
            apiQueryParams.Add("mapType", "h");
        }
        // TODO: add some handling if the map type is not what we expect


        apiQueryParams.Add("quadKey", quadkey);
        apiQueryParams.Add("mapTypeImageFormat", "JPG");
        // TODO: Do not hardcode the API version. Find a way to parametrize this
        apiQueryParams.Add("apiVersion", "563");
        apiQueryParams.Add("lang", "en");
        return apiQueryParams;
    }


    // Overridden from MapProvider. Queues up an HTTP request
    // to fetch a map tile (256x256 raw byte array).
    // If eventually successful, the RawMapTileDataReceivedEventHandler(byte[] rawMapTileData)
    // signal in MapProvider will be called
    public override Error FetchRawMapTileData(string queryString)
    {
        bool requestersAvailable = m_availableRequesters.TryDequeue(out HttpRequest httpRequester);
        Error error;
        if (requestersAvailable)
        {
            // If the request finishes, onHttpRequestCompleted() gets called
            error = httpRequester.Request(queryString);
        }
        else
        {
            error = Error.Busy;
        }

        return error;
    }

    // Overridden from MapProvider. This is called automatically by any one HttpRequest object
    // upon an HTTP request response
    public override void OnHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        EmitSignal("RawMapTileDataReceived", body);
    }

    public override int NextServerNumber()
    {
        int next = m_nextServerInstance;
        m_nextServerInstance = (++m_nextServerInstance) % m_MAX_CONCURRENT_HTTP_REQUESTS;
        return next;
    }


    // Requests a map tile from the Bing API. If successful, the
    // RawMapTileDataReceivedEventHandler(byte[] rawMapTileData) signal from the
    // MapProvider base class will be invoked where the raw image data can be "picked up"
    // latitude and longitude are in radians
    public override void RequestMapTile(float latitude, float longitude, int zoom)
    {
        double latRad = latitude * MapUtils.DEGREES_TO_RADIANS;
        double lonRad = longitude * MapUtils.DEGREES_TO_RADIANS;

        int latTileCoords = MapUtils.LatitudeToTileCoordinateMercator(latRad, zoom);
        int lonTileCoords = MapUtils.LongitudeToTileCoordinateMercator(lonRad, zoom);

        string quadkey = MapUtils.TileCoordinatesToQuadkey(lonTileCoords, latTileCoords, zoom);
        Dictionary<string, string> queryParamDict = ConstructQueryParams(quadkey);
        string queryString = ConstructQueryString(queryParamDict);

        Error error = FetchRawMapTileData(queryString);

        // Queue up to try one more time later
        // TODO: Have a clean way to adjust the number of retries
        if (error != Error.Ok)
        {
            m_pendingRequests.Enqueue(() => FetchRawMapTileData(queryString));
        }

    }


}
