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


using System.IO;
using Godot;

public class BingMapTileCacher : ICacheCapability<BingMercatorMapTile>
{

    public BingMapTileCacher()
    {

        if (!Directory.Exists(USER_CACHE_FOLDER_PATH))
        {
            Directory.CreateDirectory(USER_CACHE_FOLDER_PATH);
            GD.Print("Cache directory created at: " + USER_CACHE_FOLDER_PATH);
        }

        if (!File.Exists(USER_RESOURCE_MAP_PATH))
        {
            using (File.CreateText(USER_RESOURCE_MAP_PATH))
            { }
            GD.Print("Bing cache dictionary file created at: " + USER_RESOURCE_MAP_PATH);
        }
    }

    /// <summary>
    /// Bing map tile cache, located in the user:// directory
    /// </summary>
    private readonly string USER_CACHE_FOLDER_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache/");

    /// <summary>
    /// Contains the default high-resolution Earth textures
    /// </summary>
    private readonly string DEFAULT_CACHE_FOLDER_PATH =
        "res://Universe/SolarSystem/Planets/Earth/Assets/MapTiles";

    private readonly string USER_RESOURCE_MAP_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache", "BingResourceCacheMap.csv");

    public void CacheResource(BingMercatorMapTile resource)
    {
        throw new System.NotImplementedException();
    }

    public BingMercatorMapTile RetrieveResourceFromCache(string resourceHash)
    {
        throw new System.NotImplementedException();
    }

    public bool ResourceExists(string resourceHash)
    {
        throw new System.NotImplementedException();
    }

    public string GenerateResourcePath(BingMercatorMapTile resource)
    {
        throw new System.NotImplementedException();
    }

    public BingMercatorMapTile RetrieveResourceFromCache(BingMercatorMapTile partialResource)
    {
        // Check the pre-bundled high-resolution texture path
        string filePath = DEFAULT_CACHE_FOLDER_PATH + "/" + partialResource.Hash;
        BingMercatorMapTile bingMercatorMapTile = new BingMercatorMapTile();
        if (Godot.FileAccess.FileExists(filePath))
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            bingMercatorMapTile = new BingMercatorMapTile(
                partialResource.m_quadKey,
                partialResource.m_mapType,
                partialResource.m_language,
                partialResource.m_mapImageType,
                file.GetBuffer((long)file.GetLength())
            );
        }

        // Check user cache if the high resolution texture doesn't exist
        filePath = USER_CACHE_FOLDER_PATH + "/" + partialResource.Hash;
        if (Godot.FileAccess.FileExists(filePath))
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            bingMercatorMapTile = new BingMercatorMapTile(
                partialResource.m_quadKey,
                partialResource.m_mapType,
                partialResource.m_language,
                partialResource.m_mapImageType,
                file.GetBuffer((long)file.GetLength())
            );
        }
        return bingMercatorMapTile;
    }

    public bool ResourceExists(BingMercatorMapTile partialResource)
    {
        string filePath = DEFAULT_CACHE_FOLDER_PATH + "/" + partialResource.Hash;
        if (Godot.FileAccess.FileExists(filePath))
        {
            return true;
        }

        filePath = USER_CACHE_FOLDER_PATH + "/" + partialResource.Hash;
        if (Godot.FileAccess.FileExists(filePath))
        {
            return true;
        }

        return false;
    }
}
