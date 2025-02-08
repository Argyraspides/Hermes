
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


using System.Collections.Generic;


/// <summary>
/// This interface defines the contract for any class that wants to represent query parameters
/// for a URL. By implementing this interface, a class promises to be able to convert its
/// properties into a dictionary of string key-value pairs that can be used in a URL.
/// </summary>
public interface IQueryParameters
{

    /// <summary>
    /// This method converts the implementing class's properties into a dictionary
    /// where both keys and values are strings. This is useful for URL construction
    /// since query parameters are always string-based.
    /// </summary>
    /// <returns></returns>
    IDictionary<string, string> ToQueryDictionary();

}
