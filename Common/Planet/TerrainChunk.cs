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
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Represents a single chunk of planetary terrain in a quadtree structure.
/// Handles loading and display of map tiles from a Web Mercator projection, reprojecting them
/// onto an ellipsoidal surface. Each chunk knows its position (lat/lon in radians) and coverage area.
/// </summary>
public partial class TerrainChunk : Node3D
{
    private readonly string SHADER_PATH;
    public MapTile MapTile { get; private set; }

    /// <summary>
    /// Gets or sets the mesh that will define the geometry of the chunk.
    /// In general, if the mesh includes the poles of the planet,
    /// the mesh will be triangular. Otherwise, it will be a quadrilateral.
    /// </summary>
    public MeshInstance3D MeshInstance3D { get; private set; }


    /// <summary>
    /// Gets or sets the shader material used for map reprojection.
    /// E.g., warping a Web-Mercator projection map tile
    /// such that it can be fit to an ellipsoid.
    /// </summary>
    public ShaderMaterial ShaderMaterial { get; private set; }


    public MeshInstance3D MeshInstance
    {
        get => MeshInstance3D;
        set => MeshInstance3D = value;
    }

    /// <summary>
    /// Initializes a new instance of the TerrainChunk class.
    /// </summary>
    /// <param name="lat">Center latitude in radians.</param>
    /// <param name="lon">Center longitude in radians.</param>
    /// <param name="latRange">Latitude range covered in radians.</param>
    /// <param name="lonRange">Longitude range covered in radians.</param>
    /// <param name="zoomLevel">Map zoom level.</param>
    /// <param name="meshInstance3D">3D mesh instance for the terrain.</param>
    /// <param name="texture2D">Texture to be applied to the terrain.</param>
    public TerrainChunk(
        MapTile mapTile,
        MeshInstance3D meshInstance3D = null,
        ShaderMaterial shaderMaterial = null
    )
    {
        if (mapTile == null)
        {
            throw new ArgumentNullException("Cannot create a TerrainChunk with a null map tile");
        }

        MapTile = mapTile;
        if (mapTile.MapTileType == MapTileType.WEB_MERCATOR)
        {
            SHADER_PATH = "res://Common/Shaders/WebMercatorToWGS84Shader.gdshader";
        }

        // TODO(Argyraspides, 02/08/2025): Handle cases where the map tile is unknown
        MeshInstance3D = meshInstance3D;
        ShaderMaterial = shaderMaterial;
    }

    public async void Load()
    {
        try
        {
            AddChild(MeshInstance3D);
            await InitializeTerrainChunkAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to initialize terrain: {ex}");
        }
    }

    public void ToggleVisible(bool visible)
    {
        MeshInstance3D.Visible = visible;
    }

    /// <summary>
    /// Applies a texture to the terrain chunk's mesh and configures shader parameters
    /// for Web Mercator to WGS84 reprojection. The shader transforms the flat map
    /// projection into the correct spherical coordinates for the planet's surface.
    /// Also applies a scale correction to handle east-west texture inversion.
    /// </summary>
    /// <param name="texture2D">The texture to apply to the terrain.</param>
    private void ApplyTexture(Texture2D texture2D)
    {
        var shaderMat = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(SHADER_PATH) };
        shaderMat.SetShaderParameter("map_tile", texture2D);
        shaderMat.SetShaderParameter("zoom_level", MapTile.ZoomLevel);
        shaderMat.SetShaderParameter("tile_size", MapTile.Size);
        MeshInstance3D.MaterialOverride = shaderMat;
    }

    private async Task InitializeTerrainChunkAsync()
    {
        var mapApi = new MapAPI();
        MapTile mapTile = await mapApi.RequestMapTileAsync(
            (float)MapTile.Latitude,
            (float)MapTile.Longitude,
            MapTile.ZoomLevel,
            MapTile.MapType,
            MapTile.MapImageType
        );

        ApplyTexture(mapTile.Texture2D);
    }

    public void SetPosition()
    {
        if (MapTile == null)
        {
            throw new ArgumentNullException("Cannot set position of a terrain chunk with a null map tile");
        }

        Vector3 cartesianPos = MapUtils.LatLonToCartesian(MapTile.Latitude, MapTile.Longitude);

        float latScale = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        float lonScale = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM;

        // Terrainchunk should know its pos in ECEF space in the Godot world based on map tile lat/lon this works file
        cartesianPos.X *= latScale;
        cartesianPos.Y *= lonScale;
        cartesianPos.Z *= latScale;

        GlobalPosition = cartesianPos;

        // However when we try and also scale the mesh instances or even just the terrainchunk itself,
        // everything goes haywire. If we only scale and dont set position, everything looks fine.
        // TODO(Argyraspides, 2025-01-29): Handle east-west inversion in shader instead of mesh scale
        Scale = new Vector3(-latScale, lonScale, latScale);
    }
}
