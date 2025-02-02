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

// TODO: Add documentation on what this is. Also add a cache store to point to a location where cached map tiles are stored
// to retrieve those instead
public class BingMapProvider : MapProvider
{

    /*

    The Bing maps API query URL looks like this:

        "https://ecn.t{server}.tiles.virtualearth.net/tiles/{mapType}{quadKey}.{mapTypeImageFormat}?g={apiVersion}&mkt={lang}";

    */

    // ParamKeyValuePairs for the BingMapProvider should contain, in order:
    // Field: mapType 				("a" for satellite, "r" for street view, "h" for hybrid)
    // Field: quadKey 				(quadrant key value)
    // Field: mapTypeImageFormat    (image file type, JPG for satellite & hybrid views, PNG for street)
    // Field: apiVersion			(API version number)
    // Field: lang					(language, e.g., "en" for English)
    public class BingQueryParameters : QueryParameters
    {
        public BingQueryParameters(
            float latitude,
            float longitude,
            int zoomLevel,
            MapType mapType,
            MapUtils.MapImageType mapImageType)
        : base(latitude, longitude, zoomLevel, mapType, mapImageType)
        {
            m_CACHED_MAP_TILE_PATH_SATELLITE =
                ProjectSettings.GlobalizePath("res://") + "Common/Planet/PlanetTiles/EarthTiles/Bing";
            NextServerNumber();
        }

        public override void InitializeQueryParams()
        {
            m_QUERY_STR_PARAM_NAMES = new List<string> { "server", "mapType", "quadKey", "mapTypeImageFormat", "apiVersion", "lang" };

            m_QUERY_STR_TEMPLATE = string.Format(
                "https://ecn.t{{{0}}}.tiles.virtualearth.net/tiles/{{{1}}}{{{2}}}.{{{3}}}?g={{{4}}}&mkt={{{5}}}",
                m_QUERY_STR_PARAM_NAMES[0],
                m_QUERY_STR_PARAM_NAMES[1],
                m_QUERY_STR_PARAM_NAMES[2],
                m_QUERY_STR_PARAM_NAMES[3],
                m_QUERY_STR_PARAM_NAMES[4],
                m_QUERY_STR_PARAM_NAMES[5]
            );

            int server = m_nextServerInstance;

            int latTileCoo = MapUtils.LatitudeToTileCoordinateMercator(m_latitude, m_zoomlevel);
            int lonTileCoo = MapUtils.LongitudeToTileCoordinateMercator(m_longitude, m_zoomlevel);

            string quadKey = MapUtils.TileCoordinatesToQuadkey(latTileCoo, lonTileCoo, m_zoomlevel);

            // TODO(Argyraspides, 02/02/2025): Do not hardcode API version
            string apiVersion = "523";

            // TODO(Argyraspides, 02/02/2025): Do not hardcode language for future language support
            string lang = "en";


            string mapTypeImageFormat;
            if(m_mapImageType == MapUtils.MapImageType.JPEG)
            {
                mapTypeImageFormat = "JPG";
            }
            else if(m_mapImageType == MapUtils.MapImageType.PNG)
            {
                mapTypeImageFormat = "PNG";
            }
            else
            {
                // Default to JPG for lower image sizes
                mapTypeImageFormat = "JPG";
            }

            string mapType;
            if(m_mapType == MapType.SATELLITE)
            {
                mapType = "a";
            }
            else if(m_mapType == MapType.STREET)
            {
                mapType = "r";
            }
            else if(m_mapType == MapType.HYBRID)
            {
                mapType = "h";
            }
            else
            {
                mapType = "a";
            }


            m_paramKeyValuePair = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[0], server.ToString()),
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[1], mapType),
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[2], quadKey),
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[3], mapTypeImageFormat),
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[4], apiVersion),
                new KeyValuePair<string, string>(m_QUERY_STR_PARAM_NAMES[5], lang)
            };
        }
    };


    // Constructs a query string for obtaining a map tile from Bing's map provider API.
    // Takes in a BingMapTileParams struct which should contain mappings from the name of the
    // parameter name (string) to the value of the parameter (string)
    public override string ConstructQueryString(QueryParameters queryParameters)
    {

        StringBuilder finalQueryString = new StringBuilder(m_QUERY_STR_TEMPLATE);
        finalQueryString.Replace("{" + m_QUERY_STR_PARAM_NAMES[0] + "}", NextServerNumber().ToString());

        List<KeyValuePair<string, string>> paramKeyValuePair = queryParameters.GetParams();

        foreach (KeyValuePair<string, string> apiParamPair in paramKeyValuePair)
        {
            string replacementStr = "{" + apiParamPair.Key + "}";
            finalQueryString.Replace(replacementStr, apiParamPair.Value);
        }

        return finalQueryString.ToString();
    }

    public override QueryParameters ConstructQueryParameters(
        float latitude,
        float longitude,
        int zoomLevel,
        MapType mapType,
        MapUtils.MapImageType mapImageType
    )
    {
        m_queryParameters = new BingQueryParameters(
            latitude,
            longitude,
            zoomLevel,
            mapType,
            mapImageType
        );
        m_queryParameters.InitializeQueryParams();
        return m_queryParameters;
    }
}
