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

namespace Hermes.Common.Networking.Cache;

public interface ICacheCapability<HermesResource>
{
    void CacheResource(HermesResource resource);
    HermesResource RetrieveResourceFromCache(string resourceHash);

    /// <summary>
    /// Can be used to retrieve a resource from cache if the provided resource argument contains enough
    /// information in order to determine the identity of the full resource with all its fields.
    /// </summary>
    /// <param name="partialResource"> The resource with parts of its fields filled out which contains the necessary information
    /// to uniquely identify the entire resource</param>
    /// <returns></returns>
    HermesResource RetrieveResourceFromCache(HermesResource partialResource);

    bool ResourceExists(string resourceHash);
    bool ResourceExists(HermesResource partialResource);
}
