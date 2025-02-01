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
public partial class TerrainChunk : Node
{
    private const string SHADER_PATH = "res://Common/Shaders/WebMercatorToWGS84Shader.gdshader";

    private float m_latitude;
    private float m_latitudeRange;
    private float m_longitude;
    private float m_longitudeRange;
    private int m_zoomLevel;
    private MeshInstance3D m_meshInstance3D;
    private ShaderMaterial m_shaderMaterial;

    /// <summary>
    /// Gets or sets the latitude location of this terrain chunk in radians.
    /// Latitude is located at the center of the chunk's shape
    /// which is determined by its mesh.
    /// </summary>
    public float Latitude
    {
        get => m_latitude;
        set => m_latitude = value;
    }

    /// <summary>
    /// Gets or sets the latitude range of this terrain chunk in radians.
    /// I.e., how many degrees of latitude that the chunk covers in total.
    /// </summary>
    public float LatitudeRange
    {
        get => m_latitudeRange;
        set => m_latitudeRange = value;
    }

    /// <summary>
    /// Gets or sets the longitude location of this terrain chunk in radians.
    /// Latitude is located at the center of the chunk's shape
    /// which is determined by its mesh.
    /// </summary>
    public float Longitude
    {
        get => m_longitude;
        set => m_longitude = value;
    }

    /// <summary>
    /// Gets or sets the longitude range of this terrain chunk in radians.
    /// I.e., how many degrees of longitude that the chunk covers in total.
    /// </summary>
    public float LongitudeRange
    {
        get => m_longitudeRange;
        set => m_longitudeRange = value;
    }

    /// <summary>
    /// Gets or sets the zoom level of the terrain chunk.
    /// See: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
    /// for what zoom level means and how/why it works in the context of map tiles
    /// </summary>
    public int ZoomLevel
    {
        get => m_zoomLevel;
        set => m_zoomLevel = value;
    }

    /// <summary>
    /// Gets or sets the mesh that will define the geometry of the chunk.
    /// In general, if the mesh includes the poles of the planet,
    /// the mesh will be triangular. Otherwise, it will be a quadrilateral.
    /// </summary>
    public MeshInstance3D MeshInstance
    {
        get => m_meshInstance3D;
        set => m_meshInstance3D = value;
    }

    /// <summary>
    /// Gets or sets the shader material used for map reprojection.
    /// E.g., warping a Web-Mercator projection map tile
    /// such that it can be fit to an ellipsoid.
    /// </summary>
    public ShaderMaterial ShaderMaterial
    {
        get => m_shaderMaterial;
        set => m_shaderMaterial = value;
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
        float lat = 0.0f,
        float lon = 0.0f,
        float latRange = 0.0f,
        float lonRange = 0.0f,
        int zoomLevel = 0,
        MeshInstance3D meshInstance3D = null,
        Texture2D texture2D = null
    )
    {
        m_latitude = lat;
        m_longitude = lon;
        m_latitudeRange = latRange;
        m_longitudeRange = lonRange;
        m_zoomLevel = zoomLevel;
        m_meshInstance3D = meshInstance3D;
    }

    public async void Load()
    {
        try
        {
            AddChild(m_meshInstance3D);
            await InitializeTerrainChunkAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to initialize terrain: {ex}");
        }
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
        var shaderMat = new ShaderMaterial
        {
            Shader = ResourceLoader.Load<Shader>(SHADER_PATH)
        };
        shaderMat.SetShaderParameter("map_tile", texture2D);
        shaderMat.SetShaderParameter("zoom_level", m_zoomLevel);
        m_meshInstance3D.MaterialOverride = shaderMat;

        // TODO(dev, 2025-01-29): Handle east-west inversion in shader instead of mesh scale
        m_meshInstance3D.Scale = new Vector3(-1, 1, 1);
    }

    private async Task InitializeTerrainChunkAsync()
    {
        var mapApi = new MapAPI();
        byte[] rawMapData = await mapApi.RequestMapTileAsync(
            Mathf.RadToDeg(m_latitude),
            Mathf.RadToDeg(m_longitude),
            m_zoomLevel
        );

        ApplyTexture(MapUtils.ByteArrayToImageTexture(rawMapData));
    }
}
