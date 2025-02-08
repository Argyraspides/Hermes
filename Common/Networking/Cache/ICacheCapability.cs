public interface ICacheCapability<Resource>
{
    void CacheResource(Resource resource);
    Resource RetrieveResourceFromCache(string resourceHash);

    /// <summary>
    /// Can be used to retrieve a resource from cache if the provided resource argument contains enough
    /// information in order to determine the identity of the full resource with all its fields.
    /// </summary>
    /// <param name="partialResource"> The resource with parts of its fields filled out which contains the necessary information
    /// to uniquely identify the entire resource</param>
    /// <returns></returns>
    Resource RetrieveResourceFromCache(Resource partialResource);
    bool ResourceExists(string resourceHash);
    bool ResourceExists(Resource partialResource);
    string GenerateResourcePath(Resource resource);

}
