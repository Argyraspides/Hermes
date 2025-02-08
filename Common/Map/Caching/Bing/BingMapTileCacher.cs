using System.Collections.Generic;
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

        // LoadResourceMap();
    }

    /// <summary>
    /// Bing map tile cache, located in the user:// directory
    /// </summary>
    private readonly string USER_CACHE_FOLDER_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache/");

    /// <summary>
    /// Contains the default high-resolution Earth textures
    /// </summary>
    private readonly string DEFAULT_CACHE_FOLDER_PATH =
        "res://Universe/SolarSystem/Assets/Earth/MapTiles";

    private readonly string USER_RESOURCE_MAP_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache", "BingResourceCacheMap.csv");

    // private Dictionary<string, string> m_userResourceMap;


    // private void LoadResourceMap()
    // {
    //     m_userResourceMap = new Dictionary<string, string>();

    //     if (File.Exists(USER_RESOURCE_MAP_PATH))
    //     {
    //         foreach (var line in File.ReadLines(USER_RESOURCE_MAP_PATH))
    //         {
    //             var parts = line.Split(',');
    //             if (parts.Length == 2)
    //             {
    //                 m_userResourceMap[parts[0].Trim()] = parts[1].Trim();
    //             }
    //         }
    //         GD.Print($"Loaded {m_userResourceMap.Count} entries from cache.");
    //     }
    // }

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
    // res://Universe/SolarSystem/Assets/Earth/MapTiles/Bing/SATELLITE/png/en/6/tile_1_0.png

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
