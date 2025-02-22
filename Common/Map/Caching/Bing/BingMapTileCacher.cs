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


using System;
using System.IO;
using Godot;
using Hermes.Common.Map.Types.Bing;
using FileAccess = Godot.FileAccess;

public class BingMapTileCacher : ICacheCapability<BingMercatorMapTile>
{
    public BingMapTileCacher()
    {
        if (!Directory.Exists(USER_CACHE_FOLDER_PATH))
        {
            Directory.CreateDirectory(USER_CACHE_FOLDER_PATH);
            GD.Print("Cache directory created at: " + USER_CACHE_FOLDER_PATH);
        }
    }

    /// <summary>
    /// Bing map tile cache, located in the user:// directory. This is the path to cache all the map tiles that the
    /// user has accumulated over the runtime of Hermes
    /// </summary>
    private readonly string USER_CACHE_FOLDER_PATH = Path.Combine(OS.GetUserDataDir(), "BingMapProvider", "Cache");

    /// <summary>
    /// Contains the default high-resolution Earth textures
    /// </summary>
    private readonly string DEFAULT_CACHE_FOLDER_PATH =
        "res://Universe/SolarSystem/Planets/Earth/Assets/MapTiles";


    public void CacheResource(BingMercatorMapTile resource)
    {
        string filePathOfMapTile = Path.Combine(USER_CACHE_FOLDER_PATH, resource.ResourcePath);
        string directoryPathOfMapTile = Path.GetDirectoryName(filePathOfMapTile);
        if (!Directory.Exists(directoryPathOfMapTile))
        {
            try
            {
                Directory.CreateDirectory(directoryPathOfMapTile);
            }
            catch (Exception e)
            {
                GD.PrintErr("Error creating directory: " + directoryPathOfMapTile + "\n" + e.Message);
            }
        }

        using var file = FileAccess.Open(filePathOfMapTile, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr(
                "Unable to cache bing mercator map tile. " +
                "The file path may be invalid, you may have inappropriate permissions, " +
                "or the file is currently being accessed by another resource");
            return;
        }

        file.StoreBuffer(resource.ResourceData);
    }

    public BingMercatorMapTile RetrieveResourceFromCache(string resourceHash)
    {
        throw new NotImplementedException();
    }

    public bool ResourceExists(string resourceHash)
    {
        // Check both the user cache for map tiles cached during runtime, and default cache
        // for pre-bundled textures
        return
            File.Exists(Path.Combine(USER_CACHE_FOLDER_PATH, resourceHash)) ||
            File.Exists(Path.Combine(DEFAULT_CACHE_FOLDER_PATH, resourceHash));
    }

    public string GenerateResourcePath(BingMercatorMapTile resource)
    {
        throw new System.NotImplementedException();
    }

    public BingMercatorMapTile RetrieveResourceFromCache(BingMercatorMapTile partialResource)
    {
        if (!ResourceExists(partialResource))
        {
            throw new FileNotFoundException(
                "Unable to retrieve bing mercator map tile from cache. Map tile doesn't exist");
        }

        // Check the pre-bundled high-resolution texture path
        string filePath = Path.Combine(DEFAULT_CACHE_FOLDER_PATH, partialResource.ResourcePath);
        BingMercatorMapTile bingMercatorMapTile = new BingMercatorMapTile();
        if (Godot.FileAccess.FileExists(filePath))
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            bingMercatorMapTile = new BingMercatorMapTile(
                partialResource.QuadKey,
                partialResource.MapType,
                partialResource.Language,
                partialResource.MapImageType,
                file.GetBuffer((long)file.GetLength())
            );
        }

        // Check user cache if the high resolution texture doesn't exist
        filePath = Path.Combine(USER_CACHE_FOLDER_PATH, partialResource.Hash);
        if (Godot.FileAccess.FileExists(filePath))
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            bingMercatorMapTile = new BingMercatorMapTile(
                partialResource.QuadKey,
                partialResource.MapType,
                partialResource.Language,
                partialResource.MapImageType,
                file.GetBuffer((long)file.GetLength())
            );
        }

        return bingMercatorMapTile;
    }

    public bool ResourceExists(BingMercatorMapTile partialResource)
    {
        string filePath = Path.Combine(DEFAULT_CACHE_FOLDER_PATH, partialResource.ResourcePath);
        if (Godot.FileAccess.FileExists(filePath))
        {
            return true;
        }

        filePath = Path.Combine(USER_CACHE_FOLDER_PATH, partialResource.Hash);
        if (Godot.FileAccess.FileExists(filePath))
        {
            return true;
        }

        return false;
    }
}
