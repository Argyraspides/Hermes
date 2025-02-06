using System.Collections.Generic;
using System.IO;
using Godot;

public class BingMapTileCacher : ICacheCapability<MercatorMapTile>
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

        LoadResourceMap();
    }

    private readonly string USER_CACHE_FOLDER_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache/");

    /// <summary>
    /// Contains the default high-resolution Earth textures
    /// </summary>
    private readonly string DEFAULT_CACHE_FOLDER_PATH =
        "res://Universe/SolarSystem/Earth/Assets/EarthTiles/Bing/Satellite/ZoomLevel6/PNG";

    private readonly string USER_RESOURCE_MAP_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache", "BingResourceCacheMap.csv");

    private Dictionary<string, string> m_userResourceMap;


    private void LoadResourceMap()
    {
        m_userResourceMap = new Dictionary<string, string>();

        if (File.Exists(USER_RESOURCE_MAP_PATH))
        {
            foreach (var line in File.ReadLines(USER_RESOURCE_MAP_PATH))
            {
                var parts = line.Split(',');
                if (parts.Length == 2)
                {
                    m_userResourceMap[parts[0].Trim()] = parts[1].Trim();
                }
            }
            GD.Print($"Loaded {m_userResourceMap.Count} entries from cache.");
        }
    }

    public void CacheResource(MercatorMapTile resource)
    {
        throw new System.NotImplementedException();
    }

    public MercatorMapTile RetrieveResourceFromCache(string resourceHash)
    {
        throw new System.NotImplementedException();
    }

    public bool ResourceExists(string resourceHash)
    {
        throw new System.NotImplementedException();
    }

    public string GenerateResourcePath(MercatorMapTile resource)
    {
        throw new System.NotImplementedException();
    }
}
