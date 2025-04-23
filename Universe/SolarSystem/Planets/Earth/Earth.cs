/*




  ,ad8888ba,         db         88         db
 d8"'    `"8b       d88b        88        d88b
d8'                d8'`8b       88       d8'`8b
88                d8'  `8b      88      d8'  `8b
88      88888    d8YaaaaY8b     88     d8YaaaaY8b
Y8,        88   d8""""""""8b    88    d8""""""""8b
 Y8a.    .a88  d8'        `8b   88   d8'        `8b
  `"Y88888P"  d8'          `8b  88  d8'          `8b

                WEAVER OF THE WORLD

*/

using Hermes.Common.Map.Types;

namespace Hermes.Universe.SolarSystem.Planets.Earth;

using Godot;
using Hermes.Common.Planet;
using Hermes.Common.Planet.LoDSystem;

public partial class Earth : Planet
{
    private PlanetOrbitalCamera m_planetOrbitalCamera;

    public override void _Ready()
    {
        m_defaultZoomLevel = 6;
        base._Ready();
    }

    protected override void InitializePlanetData()
    {
        m_planetID = PlanetID.EARTH;
        m_planetShapeType = PlanetShapeType.WGS84_ELLIPSOID;
        m_semiMajorAxisKm = SolarSystemConstants.EARTH_POLAR_RADIUS_M;
        m_semiMinorAxisKm = SolarSystemConstants.EARTH_POLAR_RADIUS_KM;
    }

    protected override void InitializePlanetSurface(int zoomLevel)
    {
        InitializeCamera();

        m_terrainQuadTree = new TerrainQuadTree(m_planetOrbitalCamera, MapTileType.WEB_MERCATOR_WGS84);
        AddChild(m_terrainQuadTree);
        m_terrainQuadTree.Name = "EarthTerrainQuadTree";
        m_terrainQuadTree.InitializeQuadTree(m_defaultZoomLevel);
    }

    private void InitializeCamera()
    {
        m_planetOrbitalCamera = GetNode<PlanetOrbitalCamera>("EarthOrbitalCamera");
    }
}
