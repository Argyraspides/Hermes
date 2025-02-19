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
using Hermes.Common.Planet.LoDSystem;

public abstract partial class Planet : StaticBody3D
{
    /// <summary>
    /// For debugging purposes. Shows the wireframe of the planet's mesh
    /// </summary>
    [Export] private bool m_showWireframe = false;

    private bool m_previousWireframeState = false;


    protected PlanetID m_planetID;
    protected PlanetShapeType m_planetShapeType;

    /// <summary>
    /// Terrain quad tree represents the planets surface in a quadtree data structure
    /// as a means for a LoD (Level of Detail) system
    /// </summary>
    protected TerrainQuadTree m_terrainQuadTree;

    /// <summary>
    /// The radius of the planet in kilometers when modelled as a sphere
    /// </summary>
    protected float m_sphericalRadiusKm;

    /// <summary>
    /// Semi-major axis of the planet in kilometers when modelled as an ellipsoid.
    /// All planets are assumed to be oblate spheroids/ellipsoids during actual rendering.
    /// The semi-major axis of an ellipsoid is the length of the longest distance from
    /// the center of an ellipsoid to its surface
    /// </summary>
    protected float m_semiMajorAxisKm;


    /// <summary>
    /// Semi minor axis of the planet in kilometers when modelled as an ellipsoid.
    /// All planets are assumed to be oblate spheroids/ellipsoids during actual rendering.
    /// The semi-minor axis of an ellipsoid is the length of the smallest distance from the
    /// center of an ellipsoid to its surface
    /// </summary>
    protected float m_semiMinorAxisKm;


    /// <summary>
    /// Zoom level that this planet should start at.
    /// </summary>
    protected int m_defaultZoomLevel;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        InitializePlanetData();
        InitializePlanetSurface(m_defaultZoomLevel);
    }

    public override void _Process(double delta)
    {
        if (m_showWireframe != m_previousWireframeState)
        {
            UpdateWireframeState();
        }
    }

    protected abstract void InitializePlanetData();
    protected abstract void InitializePlanetSurface(int zoomLevel);

    protected void UpdateWireframeState()
    {
        RenderingServer.SetDebugGenerateWireframes(m_showWireframe);
        GetViewport()
            .SetDebugDraw(m_showWireframe ? Viewport.DebugDrawEnum.Wireframe : Viewport.DebugDrawEnum.Disabled);
        m_previousWireframeState = m_showWireframe;
    }
}
