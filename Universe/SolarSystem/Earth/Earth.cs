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
using System.Threading;
using System.Threading.Tasks;

public partial class Earth : StaticBody3D
{
    [Export]
    private bool showWireframe = false;
    private bool previousWireframeState = false;

    private TerrainQuadTree terrainQuadTree;

    private Texture2D texture2D;


    private void UpdateWireframeState()
    {
        RenderingServer.SetDebugGenerateWireframes(showWireframe);
        GetViewport().SetDebugDraw(showWireframe ?
            Viewport.DebugDrawEnum.Wireframe :
            Viewport.DebugDrawEnum.Disabled);
        previousWireframeState = showWireframe;
    }

    // Generates a bunch of TerrainChunks that will create a WGS84 ellipsoid
    // and adds them to the scene tree. By default, the tiles applied to the Earth
    // are at zoom level 5 (2^5 tiles each side)
    private void GenerateEarthMesh(int zoomLevel = 6)
    {

        TerrainQuadTree terrainQuadTree = new TerrainQuadTree();
        terrainQuadTree.InitializeQuadTree(zoomLevel);

        for (int i = 0; i < Math.Pow(4, zoomLevel - 1); i++)
        {
            TerrainQuadTree.TerrainQuadTreeNode terrainQuadTreeNode =
                 terrainQuadTree.TerrainQuadTreeNodes[terrainQuadTree.TerrainQuadTreeNodes.Count - 1][i];

            ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
                terrainQuadTreeNode.Chunk.Latitude,
                terrainQuadTreeNode.Chunk.Longitude,
                terrainQuadTreeNode.Chunk.LatitudeRange,
                terrainQuadTreeNode.Chunk.LongitudeRange
            );

            var meshInstance = new MeshInstance3D { Mesh = meshSegment };
            terrainQuadTreeNode.Chunk.MeshInstance = meshInstance;
            terrainQuadTreeNode.Chunk.Name = $"TerrainChunk_z{zoomLevel}_tn{i}";
            terrainQuadTreeNode.Chunk.AutoLoad = true;
            AddChild(terrainQuadTreeNode.Chunk);
        }

    }

    public override void _Ready()
    {
        UpdateWireframeState();
        GenerateEarthMesh();
    }

    public override void _Process(double delta)
    {
        if (showWireframe != previousWireframeState)
        {
            UpdateWireframeState();
        }
    }

}
