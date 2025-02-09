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

    public void OnOrbitalCameraPosChangedSignal(Vector3 position)
    {
    }
}
