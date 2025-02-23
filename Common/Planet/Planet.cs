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

namespace Hermes.Common.Planet;

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
