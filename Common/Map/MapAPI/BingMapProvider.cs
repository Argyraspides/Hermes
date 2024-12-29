using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class BingMapProvider : MapProvider
{

	string queryStrTemplate = "https://ecn.t{server}.tiles.virtualearth.net/tiles/{mapType}{quadKey}.{mapTypeImageFormat}?g={apiVersion}&mkt={lang}";

	// Constructs a query string for obtaining a map tile from Bing's map provider API.
	// Takes in a dictionary which should contain mappings from the name of the 
	// parameter name (string) to the value of the parameter (string)
	// Dictionary keys must include:
	//
	// "server" 				(server instance to query, between 0-3)
	// "mapType" 				("a" for satellite, "r" for street view, "h" for hybrid)
	// "quadKey" 				(quadrant key value)
	// "mapTypeImageFormat"		(image file type, JPG for satellite & hybrid views, PNG for street)
	// "apiVersion"				(API version number)
	// "lang"					(language, e.g., "en" for English)
    public override string ConstructQueryString(Dictionary<string, string> dict)
    {

		StringBuilder finalQueryString = new StringBuilder(queryStrTemplate);

		foreach(KeyValuePair<string, string> apiParamPair in dict)
		{
			string replacementStr = "{" + apiParamPair.Key + "}";
			finalQueryString.Replace(replacementStr, apiParamPair.Value);
		}

		return finalQueryString.ToString();
    }

}
