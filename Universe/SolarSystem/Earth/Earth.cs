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
    // TODO: for testing. Remove later
    private string testTilePath = "res://Universe/SolarSystem/Assets/a03333_r.jpeg";


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
    private void GenerateEarthMesh(int zoomLevel = 4)
    {
        // # of tiles in Y direction is 2^zoomLevel
        int tileCountY = 1 << zoomLevel;
        // # of tiles in X direction is 2^(zoomLevel+1)
        int tileCountX = tileCountY * 2;

        for (int ty = 0; ty < tileCountY / 2; ty++)
        {
            for (int tx = 0; tx < tileCountX / 2; tx++)
            {
                // Get bounding lat/lon from WebMercator tile coordinates
                (double latMinDeg, double latMaxDeg, double lonMinDeg, double lonMaxDeg)
                    = MapUtils.GetTileLatLonBounds(tx, ty, zoomLevel);

                float latMin = Mathf.DegToRad((float)latMinDeg);
                float latMax = Mathf.DegToRad((float)latMaxDeg);
                float lonMin = Mathf.DegToRad((float)lonMinDeg);
                float lonMax = Mathf.DegToRad((float)lonMaxDeg);

                float centerLat = 0.5f * (latMin + latMax);
                float centerLon = 0.5f * (lonMin + lonMax);
                float latRange = latMax - latMin;
                float lonRange = lonMax - lonMin;

                ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
                    centerLat, centerLon, latRange, lonRange
                );

                var meshInstance = new MeshInstance3D { Mesh = meshSegment };

                TerrainChunk terrainChunk = new TerrainChunk(
                    centerLat, centerLon,
                    latRange, lonRange,
                    zoomLevel,
                    meshInstance,
                    null
                );
                terrainChunk.Name = $"TerrainChunk_z{zoomLevel}_x{tx}_y{ty}";
                AddChild(terrainChunk);
            }
        }

    }

    public override void _Ready()
    {
        texture2D = GD.Load<Texture2D>(testTilePath);
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