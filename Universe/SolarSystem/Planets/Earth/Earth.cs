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
using Hermes.Common.Planet.LoDSystem;

public partial class Earth : Planet
{
    private PlanetOrbitalCamera m_planetOrbitalCamera;

    [Export] private Vector3 m_nullIsland;


    public override void _Ready()
    {
        InitializeCamera();
        m_defaultZoomLevel = 6;
        base._Ready();
    }

    protected override void InitializePlanetData()
    {
        m_planetID = PlanetID.EARTH;
        m_planetShapeType = PlanetShapeType.WGS84_ELLIPSOID;
        m_semiMajorAxisKm = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM;
        m_semiMinorAxisKm = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM;
    }

    protected override void InitializePlanetSurface(int zoomLevel)
    {
        m_terrainQuadTree = new TerrainQuadTree(m_planetOrbitalCamera);
        AddChild(m_terrainQuadTree);
        m_terrainQuadTree.Name = "EarthTerrainQuadTree";
        m_terrainQuadTree.InitializeQuadTree(m_defaultZoomLevel);
    }

    private void InitializeCamera()
    {
        m_planetOrbitalCamera = GetNode<PlanetOrbitalCamera>("EarthOrbitalCamera");
        Vector3 camPos = new Vector3(-SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * 10, 0, 0);
        m_planetOrbitalCamera.InitializeCameraPosition(camPos);
    }
}
