/*




     ,ad8888ba,         db         88         db
    d8"'    `"8b       d88b        88        d88b
   d8'                d8'`8b       88       d8'`8b
   88                d8'  `8b      88      d8'  `8b
   88      88888    d8YaaaaY8b     88     d8YaaaaY8b
   Y8,        88   d8""""""""8b    88    d8""""""""8b
    Y8a.    .a88  d8'        `8b   88   d8'        `8b
     `"Y88888P"  d8'          `8b  88  d8'          `8b

                     WEAVER OF WORLDS

*/

using Hermes.Common.Map.Querying;
using Hermes.Common.Map.Types;
using Hermes.Common.Map.Utils;

namespace Hermes.Common.Planet.LoDSystem;

using System;
using System.Threading.Tasks;
using Godot;
using Hermes.Common.GodotUtils;

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
        else if (mapTile.MapTileType == MapTileType.UNKNOWN)
        {
            SHADER_PATH = null;
            throw new Exception("Cannot create a TerrainChunk as the map tile type is unknown");
        }

        MeshInstance3D = meshInstance3D;
        ShaderMaterial = shaderMaterial;
    }

    public async void Load()
    {
        try
        {
            if (GodotUtils.IsValid(MeshInstance3D))
            {
                AddChild(MeshInstance3D);
                await InitializeTerrainChunkAsync();
            }
            else
            {
                throw new Exception("MeshInstance3D is not a valid MeshInstance3D");
            }
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
        if (!string.IsNullOrEmpty(SHADER_PATH))
        {
            var shaderMat = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(SHADER_PATH) };
            shaderMat.SetShaderParameter("map_tile", texture2D);
            shaderMat.SetShaderParameter("zoom_level", MapTile.ZoomLevel);
            shaderMat.SetShaderParameter("tile_width", MapTile.Width);
            shaderMat.SetShaderParameter("tile_height", MapTile.Height);

            if (GodotUtils.IsValid(MeshInstance3D))
            {
                MeshInstance3D.MaterialOverride = shaderMat;
            }
            else
            {
                throw new ArgumentNullException("MeshInstance3D is not a valid MeshInstance3D");
            }
        }
        else
        {
            GD.PrintErr("Shader material path is null or empty! Using default shader.");
            var standardShader = new StandardMaterial3D();
            if (GodotUtils.IsValid(MeshInstance3D))
            {
                MeshInstance3D.MaterialOverride = standardShader;
            }
            else
            {
                throw new ArgumentNullException("MeshInstance3D is not a valid MeshInstance3D");
            }
        }
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

        if (mapTile == null)
        {
            throw new Exception("Failed to initialize map tile");
        }

        ApplyTexture(mapTile.Texture2D);
    }

    public void SetPositionAndSize()
    {
        if (MapTile == null)
        {
            throw new ArgumentNullException("Cannot set position of a terrain chunk with a null map tile");
        }

        GlobalPosition = MapUtils.LatLonToCartesianNormalized(MapTile.Latitude, MapTile.Longitude);
        Transform = Transform.Scaled(
                new Vector3(
                    SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM,
                    SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM,
                    SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM
                ));
    }
}
