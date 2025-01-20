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
using System.Collections.Concurrent;
using System.Collections.Generic;

// TODO: A caching system should be implemented so that if a query string has already been
// used, the map tile associated with that query string is fetched locally. Dictionary mapping
// from URLs to user:// resource is a solution. And every time a map tile is fetched, it is cached
// as such. The cache should work offline and some file should be stored with mappings

// TODO: Add documentation on what this is
public partial class MapProvider : Node
{
    // Constants
    protected const int m_MAX_CONCURRENT_HTTP_REQUESTS = 4;

    // Member fields
    protected HttpRequest[] m_httpRequesters;

    // A queue of functions to call when HTTP requests have been completed.
    // This is specifically holding OnHttpRequestComplete functions to call
    protected ConcurrentQueue<Action> m_receivedMapDataSignalQueue;
    // A queue of available HttpRequest objects
    protected ConcurrentQueue<HttpRequest> m_availableRequesters;
    // A queue of pending requests for map tiles
    protected ConcurrentQueue<Action> m_pendingRequests;

    // Properties
    protected MapUtils.MapType m_mapType { get; set; }
    protected MapUtils.MapImageType m_mapImageType { get; set; }


    public MapProvider()
    {
        m_httpRequesters = new HttpRequest[m_MAX_CONCURRENT_HTTP_REQUESTS];
        m_availableRequesters = new ConcurrentQueue<HttpRequest>();
        m_receivedMapDataSignalQueue = new ConcurrentQueue<Action>();
        m_pendingRequests = new ConcurrentQueue<Action>();

        // We assign each HttpRequester objects "RequestCompleted" event a lambda function that will queue up a signal call
        // that signals an HTTP request has been completed. Signals by default are not thread safe and its possible
        // signals may be lost if they are called at the same time.
        for (int i = 0; i < m_MAX_CONCURRENT_HTTP_REQUESTS; i++)
        {
            HttpRequest requester = new HttpRequest();
            m_httpRequesters[i] = requester;

            requester.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
            {
                m_receivedMapDataSignalQueue.Enqueue(() => OnHttpRequestCompleted(result, responseCode, headers, body));
                m_availableRequesters.Enqueue(requester);
            };

            AddChild(requester);
            m_availableRequesters.Enqueue(requester);
        }
    }

    // This signal is called when FetchRawMapTileData is successful and provides
    // the raw byte array for the map tile image data
    [Signal]
    public delegate void RawMapTileDataReceivedEventHandler(byte[] rawMapTileData);

    // Constructs the query string (URL) to obtain a map tile
    // Takes in a dictionary that maps API parameter names to their values
    public virtual string ConstructQueryString(Dictionary<string, string> dict)
    {
        return "";
    }

    // Performs an HTTP request to retrieve raw map tile data from an API provider
    // with the query string.
    public virtual Error FetchRawMapTileData(string queryString)
    {
        return Error.Ok;
    }


    // Called when any instance of the httpRequesters successfully return an HTTP response
    // with the full HTTP response result, code, headers, and body
    public virtual void OnHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
    }

    // Many API providers have different server instances you can query denoted by a unique ID.
    // Retrieves the next server instance that should be queried for the API request
    public virtual int NextServerNumber()
    {
        return 0;
    }

    public void ProcessConcurrentQueue(ConcurrentQueue<Action> queue)
    {
        while (queue.TryDequeue(out Action action))
        {
            action.Invoke();
        }
    }

    public virtual void RequestMapTile(float latitude, float longitude, int zoom)
    {
    }


    public override void _Process(double delta)
    {
        ProcessConcurrentQueue(m_receivedMapDataSignalQueue);
        ProcessConcurrentQueue(m_pendingRequests);
    }

}
