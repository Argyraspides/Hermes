using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class BingMapProvider : MapProvider
{

	int nextServerInstance = 0;

	string queryStrTemplate = "https://ecn.t{server}.tiles.virtualearth.net/tiles/{mapType}{quadKey}.{mapTypeImageFormat}?g={apiVersion}&mkt={lang}";

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

		StringBuilder finalQueryString = new StringBuilder(queryStrTemplate);
		finalQueryString.Replace("{server}", NextServerNumber().ToString());

		foreach(KeyValuePair<string, string> apiParamPair in dict)
		{
			string replacementStr = "{" + apiParamPair.Key + "}";
			finalQueryString.Replace(replacementStr, apiParamPair.Value);
		}

		return finalQueryString.ToString();
    }


	// Overridden from MapProvider
    public override Error FetchRawMapTileData(string queryString)
    {
        bool requestersAvailable = availableRequesters.TryDequeue(out HttpRequest httpRequester);
		Error error;
		if(requestersAvailable)
		{
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
	public override void onHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		EmitSignal("RawMapTileDataReceived", body);
	}

	public override int NextServerNumber()
	{
		int next = nextServerInstance;
		nextServerInstance = (++nextServerInstance) % MAX_CONCURRENT_HTTP_REQUESTS;
		return next;
	}

}
