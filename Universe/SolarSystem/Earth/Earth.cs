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

    private void UpdateWireframeState()
    {
        RenderingServer.SetDebugGenerateWireframes(showWireframe);
        GetViewport().SetDebugDraw(showWireframe ?
            Viewport.DebugDrawEnum.Wireframe :
            Viewport.DebugDrawEnum.Disabled);
        previousWireframeState = showWireframe;
    }

    private void GenerateEarthMesh(int segmentCount = 18)
    {
        // We divide the sphere into equal-sized segments
        float latitudeRange = Mathf.Pi / segmentCount;        // 180° / segmentCount
        float longitudeRange = 2.0f * Mathf.Pi / segmentCount;   // 360° / segmentCount

        // Create segments for each latitude band, starting from south pole to north pole
        for (int lat = 0; lat < segmentCount; lat++)
        {
            // Calculate the center latitude for this band
            // Start at -90° (South pole) and work up to +90° (North pole)
            float centerLat = (-Mathf.Pi / 2.0f) + (lat * latitudeRange) + (0.5f * latitudeRange);

            // Create segments around this latitude band
            for (int lon = 0; lon < segmentCount; lon++)
            {
                // Calculate the center longitude for this segment
                // Start at -180° and work around to +180°
                float centerLon = -Mathf.Pi + (lon * longitudeRange) + (0.5f * longitudeRange);

                // Create the mesh segment
                var meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
                    centerLat,
                    centerLon,
                    latitudeRange,
                    longitudeRange
                );

                // Create a MeshInstance3D to display the mesh
                var meshInstance = new MeshInstance3D();
                meshInstance.Mesh = meshSegment;

                meshInstance.Name = $"EllipsoidSegment_Lat{lat}_Lon{lon}";

                AddChild(meshInstance);
            }
        }
    }

}