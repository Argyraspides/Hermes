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
    private void GenerateEarthMesh(int zoomLevel = 5)
    {

        // There should be double the amount of longitude segments as the longitude range 
        // is double that of the latitude range (-90° to +90° for lat, -180° to +180° for lon).
        int latSegmentCount = (1 << zoomLevel);
        int lonSegmentCount = (1 << zoomLevel) * 2;
        
        float latitudeRange = Mathf.Pi / latSegmentCount;
        float longitudeRange = (2.0f * Mathf.Pi) / lonSegmentCount;

        // Create segments for each latitude band, starting from south pole to north pole
        for (int lat = 0; lat < latSegmentCount; lat++)
        {
            // Calculate the center latitude for this band
            // Start at -90° (South pole) and work up to +90° (North pole)
            float centerLat = (-Mathf.Pi / 2.0f) + (lat * latitudeRange) + (0.5f * latitudeRange);

            // Create segments around this latitude band
            for (int lon = 0; lon < lonSegmentCount; lon++)
            {   
                // Calculate the center longitude for this segment
                // Start at -180° and work around to +180°
                float centerLon = -Mathf.Pi + (lon * longitudeRange) + (0.5f * longitudeRange);

                // Create the mesh segment
                ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
                    centerLat,
                    centerLon,
                    latitudeRange,
                    longitudeRange
                );

                // Create a MeshInstance3D to display the mesh
                MeshInstance3D meshInstance = new MeshInstance3D();
                meshInstance.Mesh = meshSegment;

                TerrainChunk terrainChunk = new TerrainChunk(
                    centerLat,
                    centerLon,
                    latitudeRange,
                    longitudeRange,
                    zoomLevel,
                    meshInstance,
                    texture2D       // TODO: remove later. TerrainChunk will automatically fetch the tile corresponding to it
                    
                );

                terrainChunk.Name = $"TerrainChunk_WGS84EllipsoidSegment_SegCount_({latSegmentCount},{lonSegmentCount})_Lat{lat}_Lon{lon}";

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