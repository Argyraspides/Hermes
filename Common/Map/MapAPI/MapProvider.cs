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
using System.Text;

// TODO: Add documentation on what this is. Also add a cache store to point to a location where cached map tiles are stored
// to retrieve those instead
public abstract class MapProvider
{

    protected MapType m_mapType;
    protected static string m_CACHED_MAP_TILE_PATH_SATELLITE;
    protected static string m_CACHED_MAP_TILE_PATH_STREET;
    protected static List<string> m_QUERY_STR_PARAM_NAMES;
    protected static string m_QUERY_STR_TEMPLATE;

    protected static int m_nextServerInstance = 0;

    protected const int m_MAX_SERVERS = 4;

    protected QueryParameters m_queryParameters;


    /// <summary>
    /// Object that will hold query parameters for the specific map provider
    /// </summary>
    public class QueryParameters
    {
        public QueryParameters(float latitude, float longitude, int zoomLevel, MapType mapType, MapUtils.MapImageType mapImageType)
        {
            m_latitude = latitude;
            m_longitude = longitude;
            m_zoomlevel = zoomLevel;
            m_mapType = mapType;
            m_mapImageType = mapImageType;
        }

        protected float m_latitude;
        protected float m_longitude;
        protected int m_zoomlevel;
        protected MapType m_mapType;

        protected MapUtils.MapImageType m_mapImageType;

        protected List<KeyValuePair<string, string>> m_paramKeyValuePair;
        public virtual void InitializeQueryParams() {}
        public List<KeyValuePair<string, string>> GetParams()
        {
            return m_paramKeyValuePair;
        }
    };


    // Constructs a query string for obtaining a map tile from Bing's map provider API.
    // Takes in a BingMapTileParams struct which should contain mappings from the name of the
    // parameter name (string) to the value of the parameter (string)
    public abstract string ConstructQueryString(QueryParameters queryParameters);

    public abstract QueryParameters ConstructQueryParameters(
        float latitudeDeg,
        float longitudeDeg,
        int zoomLevel,
        MapType mapType,
        MapUtils.MapImageType mapImageType
    );

    public static int NextServerNumber()
    {
        int next = m_nextServerInstance;
        m_nextServerInstance = (++m_nextServerInstance) % m_MAX_SERVERS;
        return next;
    }

}
