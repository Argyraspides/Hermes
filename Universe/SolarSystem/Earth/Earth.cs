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


using Godot;
using System;

public partial class Earth : Planet
{


    public override void _Ready()
    {
        m_planetID = PlanetID.EARTH;
        m_planetShapeType = PlanetShapeType.WGS84_ELLIPSOID;
        m_defaultZoomLevel = 4;

        base._Ready();
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

        var finalQuadTreeLevel = m_terrainQuadTree.GetLastQuadTreeLevel();

        for (int i = 0; i < finalQuadTreeLevel.Count; i++)
        {
            var terrainQuadTreeNode = finalQuadTreeLevel[i];

            ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
                terrainQuadTreeNode.Chunk.Latitude,
                terrainQuadTreeNode.Chunk.Longitude,
                terrainQuadTreeNode.Chunk.LatitudeRange,
                terrainQuadTreeNode.Chunk.LongitudeRange
            );

            terrainQuadTreeNode.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
            terrainQuadTreeNode.Chunk.Name = $"TerrainChunk_z{zoomLevel}_tn{i}";

        }
    }

    public override void LoadPlanet()
    {
        foreach (var terrainQuadTreeNode in m_terrainQuadTree.GetLastQuadTreeLevel())
        {
            terrainQuadTreeNode.Chunk.Load();
            AddChild(terrainQuadTreeNode.Chunk);
        }
    }
}
