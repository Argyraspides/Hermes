using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

public partial class MapProvider : Node
{
	protected const int MAX_CONCURRENT_HTTP_REQUESTS = 4;

	enum MapType
	{
		SATELLITE,
		STREET,
		HYBRID
	}

	public HttpRequest[] httpRequesters;

	// A queue of functions to call when HTTP requests have been completed.
	// This is specifically holding onHttpRequestComplete functions to call
	public ConcurrentQueue<Action> receivedMapDataSignalQueue;

	// A queue of available HttpRequest objects
	public ConcurrentQueue<HttpRequest> availableRequesters;

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
	public virtual void onHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
	}

	// Many API providers have different server instances you can query denoted by a unique ID.
	// Retrieves the next server instance that should be queried for the API request
	public virtual int NextServerNumber()
	{
		return 0;
	}

	// Invokes all onHttpRequestCompleted functions that were queued up when an HTTP
	// request was successful
	public void processMapDataSignalQueue()
	{
		while (receivedMapDataSignalQueue.TryDequeue(out Action action))
		{
			action.Invoke();
		}
	}

	public override void _Process(double delta)
	{
		processMapDataSignalQueue();
	}

	public override void _Ready()
	{

		httpRequesters = new HttpRequest[MAX_CONCURRENT_HTTP_REQUESTS];
		availableRequesters = new ConcurrentQueue<HttpRequest>();
		receivedMapDataSignalQueue = new ConcurrentQueue<Action>();

		// We assign each HttpRequester objects "RequestCompleted" event a lambda function that will queue up a signal call
		// that signals an HTTP request has been completed. Signals by default are not thread safe and its possible
		// signals may be lost if they are called at the same time.
		for (int i = 0; i < MAX_CONCURRENT_HTTP_REQUESTS; i++)
		{
			HttpRequest requester = new HttpRequest();
			httpRequesters[i] = requester;

			requester.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
			{
				receivedMapDataSignalQueue.Enqueue(() => onHttpRequestCompleted(result, responseCode, headers, body));
				availableRequesters.Enqueue(requester);
			};

			AddChild(requester);
			availableRequesters.Enqueue(requester);
		}
	}

}
