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


using System.Linq;
using Godot;

public partial class Earth : Planet
{
    private PlanetOrbitalCamera m_planetOrbitalCamera;

    [Export] private Vector3 m_nullIsland;

    float[] lodSplitThresholds =
    {
        80000f, // Zoom Level 0
        40000f, // Zoom Level 1
        20000f, // Zoom Level 2
        10000f, // Zoom Level 3
        5000f, // Zoom Level 4
        2500f, // Zoom Level 5
        1250f, // Zoom Level 6
        600f, // Zoom Level 7
        300f, // Zoom Level 8
        150f, // Zoom Level 9
        75f, // Zoom Level 10
        38f, // Zoom Level 11
        19f, // Zoom Level 12
        9.5f, // Zoom Level 13
        4.8f, // Zoom Level 14
        2.4f, // Zoom Level 15
        1.2f, // Zoom Level 16
        0.6f, // Zoom Level 17
        0.3f, // Zoom Level 18
        0.15f // Zoom Level 19
    };

    private int m_currentZoomLevel = 6;


    public override void _Ready()
    {
        m_planetID = PlanetID.EARTH;
        m_planetShapeType = PlanetShapeType.WGS84_ELLIPSOID;
        m_defaultZoomLevel = 6;
        base._Ready();

        InitializeCamera();
    }

    protected override void InitializePlanetDimensions()
    {
        m_semiMajorAxisKm = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        m_semiMinorAxisKm = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM;
    }

    protected override void InitializePlanetSurface(int zoomLevel)
    {
        m_terrainQuadTree = new TerrainQuadTree();
        m_terrainQuadTree.InitializeQuadTree(zoomLevel);
        m_currentZoomLevel = zoomLevel;
    }

    private void InitializeCamera()
    {
        m_planetOrbitalCamera = GetNode<PlanetOrbitalCamera>("EarthOrbitalCamera");
        m_planetOrbitalCamera.OrbitalCameraPosChanged += OnOrbitalCameraPosChangedSignal;

        var nullIslandNode = m_terrainQuadTree.GetCenter(m_defaultZoomLevel - 1);

        var surfaceArrays = nullIslandNode.Chunk.MeshInstance.Mesh.SurfaceGetArrays((int)Mesh.ArrayType.Vertex);

        Vector3[] vertices = surfaceArrays[0].AsVector3Array();

        m_nullIsland =
            vertices.Aggregate(Vector3.Zero, (currTotalVec, currVec) => currTotalVec + currVec) / vertices.Length;

        // TODO(Argyraspides, 08/02/2025): This is somewhat of a deep issue. I suspect that the mesh generator when using the trig functions
        // is going counterclockwise/clockwise, while whatever direction is used to measure longitude is going in the oppostie direction. Meaning
        // that our null island point (lat/lon of 0,0) is actually on the opposite side (0, 2pi), so we have to flip it here to actually face
        // Africa off the coast of Guinea. Make sure to fix this. This may have implications as well with the map shader as
        // that currently needs to be manually flipped inside of TerrainChunk rather than being taken care of in the shader
        // directly
        // Camera will be directly on null island. Push back 15,000km
        m_planetOrbitalCamera.InitializeCameraPosition(-m_nullIsland + new Vector3(-15000, 0, 0));
    }

    public override void LoadPlanet()
    {
        AddChild(m_terrainQuadTree);
    }

    // TODO: Bruh idek anymore just do this tomorrow im too tired
    public void OnOrbitalCameraPosChangedSignal(Vector3 position)
    {
        float minLatRange = m_planetOrbitalCamera.CurrentLat - m_planetOrbitalCamera.ApproxVisibleLatRadius;
        float maxLatRange = m_planetOrbitalCamera.CurrentLat + m_planetOrbitalCamera.ApproxVisibleLatRadius;

        float minLonRange = m_planetOrbitalCamera.CurrentLon - m_planetOrbitalCamera.ApproxVisibleLonRadius;
        float maxLonRange = m_planetOrbitalCamera.CurrentLon + m_planetOrbitalCamera.ApproxVisibleLonRadius;

        int minLatTile = MapUtils.LatitudeToTileCoordinateMercator(minLatRange, m_currentZoomLevel);
        int maxLatTile = MapUtils.LatitudeToTileCoordinateMercator(maxLatRange, m_currentZoomLevel);

        int minLonTile = MapUtils.LongitudeToTileCoordinateMercator(minLonRange, m_currentZoomLevel);
        int maxLonTIle = MapUtils.LongitudeToTileCoordinateMercator(maxLonRange, m_currentZoomLevel);

        for (int i = minLatTile; i <= maxLatTile; i++)
        {
            for (int j = minLonTile; j <= maxLonTIle; j++)
            {
                m_terrainQuadTree.SplitAndLoadQuadTreeNode(5, 5, 5);
            }
        }
    }
}
