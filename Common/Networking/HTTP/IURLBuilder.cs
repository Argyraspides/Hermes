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

namespace Hermes.Common.Networking.HTTP;

/// <summary>
/// This interface defines a contract for classes that build URLs. The TParameters
/// type parameter must be a class that implements IQueryParameters.
/// </summary>
/// <typeparam name="TParameters">A type parameter that must be a class which implements IQueryParameters.</typeparam>
public interface IUrlBuilder<TParameters> where TParameters : IQueryParameters
{
    /// <summary>
    /// This method takes query parameters and returns a complete URL as a string
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    string BuildUrl(TParameters parameters);
}
