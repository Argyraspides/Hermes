
using System.Collections.Generic;

public interface ICacheCapability<Resource>
{
    void CacheResource(Resource resource);
    Resource RetrieveResourceFromCache(string resourceHash);
    bool ResourceExists(string resourceHash);
    string GenerateResourcePath(Resource resource);

}
