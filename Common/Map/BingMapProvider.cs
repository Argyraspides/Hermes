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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// TODO: Add documentation on what this is. Also add a cache store to point to a location where cached map tiles are stored
// to retrieve those instead
public class BingMapProvider : MapProvider
{

    public BingMapProvider()
    {
        m_CACHED_MAP_TILE_PATH =
                ProjectSettings.GlobalizePath("res://") + "Common/Planet/PlanetTiles/EarthTiles/Bing/";
    }

    /*

        The Bing maps API query URL looks like this:

        "https://ecn.t{server}.tiles.virtualearth.net/tiles/{mapType}{quadKey}.{mapTypeImageFormat}?g={apiVersion}&mkt={lang}";

    */

    // Constructs a query string for obtaining a map tile from Bing's map provider API.
    // Takes in a BingMapTileParams struct which should contain mappings from the name of the
    // parameter name (string) to the value of the parameter (string)

    // TODO(Argyraspides, 05/02/2025) Building URLs should be done by another class which has this dedicated functionality.
    // Also, right now the QueryParameters object forces you to have lat/lon/image type/etc. There should be a generic QueryParameters type
    // that lets you define whatever you want to be in a query parameter
    // So the fix would be:
    // - Make some sort of URL builder interface which defines functions for building URLs, and a generic QueryParameters object. This
    // URL builder will be generic and in its own folder tucked away for reuse for any other project
    // - In the MapProvider part of the project, make a subfolder where, just like now, BingMapProvider extends MapProvider and also contains a
    // BingURLMapProviderBuilder which implements the URL building interface. Then, BingMapProvider can use this to build its own query string using
    // a BingQueryParameters object which extends the generic QueryParameters object
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

    public async override Task<byte[]> RequestMapTileAsync(QueryParameters queryParameters)
    {
        // Check if the map tile already is cached
        string cachedUrl = CheckCachedMapTile(queryParameters);

        // If not, query Bing (Microsoft is the GOAT right guys?)
        if (cachedUrl.Length == 0)
        {
            string url = ConstructQueryString(queryParameters);
            byte[] data = await m_client.GetByteArrayAsync(url);
            return data;
        }

        // Else grab the cached map tile
        using var file = Godot.FileAccess.Open(cachedUrl, Godot.FileAccess.ModeFlags.Read);
        return file.GetBuffer((long) file.GetLength());
    }

    public override string CheckCachedMapTile(QueryParameters queryParameters)
    {
        var (lat, lon) = GetTileCoordinates(queryParameters);
        var path = BuildTilePath(queryParameters, lat, lon);

        return File.Exists(path) ? path : string.Empty;
    }

    private (int lat, int lon) GetTileCoordinates(QueryParameters queryParameters)
    {
        return (
            MapUtils.LatitudeToTileCoordinateMercator(queryParameters.Latitude, queryParameters.ZoomLevel),
            MapUtils.LongitudeToTileCoordinateMercator(queryParameters.Longitude, queryParameters.ZoomLevel)
        );
    }

    // TODO(Argyraspides, 02/02/2025): Make sure to handle every single case here for
    // map type, map image type, etc.
    private string BuildTilePath(QueryParameters queryParameters, int lat, int lon)
    {
        var mapTypeFolder = queryParameters.MapType switch
        {
            MapType.SATELLITE => "Satellite",
            MapType.STREET => "Street",
            MapType.HYBRID => "Hybrid",
            _ => throw new System.ArgumentException("Invalid map type")
        };

        var imageTypeFolder = queryParameters.MapImageType switch
        {
            MapUtils.MapImageType.PNG => "PNG",
            MapUtils.MapImageType.JPEG => "JPG",
            _ => throw new System.ArgumentException("Invalid image type")
        };

        var fileName = $"tile_{lon}_{lat}.png";

        return Path.Combine(
            m_CACHED_MAP_TILE_PATH,
            mapTypeFolder,
            "ZoomLevel" + queryParameters.ZoomLevel.ToString(),
            imageTypeFolder,
            fileName
        );
    }


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

            string quadKey = MapUtils.TileCoordinatesToQuadkey(lonTileCoo, latTileCoo, m_zoomlevel);

            // TODO(Argyraspides, 02/02/2025): Do not hardcode API version
            string apiVersion = "523";

            // TODO(Argyraspides, 02/02/2025): Do not hardcode language for future language support
            string lang = "en";


            string mapTypeImageFormat;
            if (m_mapImageType == MapUtils.MapImageType.JPEG)
            {
                mapTypeImageFormat = "JPG";
            }
            else if (m_mapImageType == MapUtils.MapImageType.PNG)
            {
                mapTypeImageFormat = "PNG";
            }
            else
            {
                // Default to JPG for lower image sizes
                mapTypeImageFormat = "JPG";
            }

            string mapType;
            if (m_mapType == MapType.SATELLITE)
            {
                mapType = "a";
            }
            else if (m_mapType == MapType.STREET)
            {
                mapType = "r";
            }
            else if (m_mapType == MapType.HYBRID)
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

            NextServerNumber();
        }
    };

}
