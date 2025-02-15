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


using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

public partial class TerrainQuadTree : Node
{
    private PlanetOrbitalCamera m_camera;
    private TerrainQuadTreeNode m_rootNode;

    private long m_maxNodes;

    // Start cleanup of nodes when we hit 75% of max capacity
    private float m_maxNodesCleanupThreshold = 0.75F;

    private int m_minDepth;
    private int m_maxDepth;

    private double[] m_splitThresholds;
    private double[] m_mergeThresholds;

    private Thread m_updateQuadTreeThread;

    private readonly object m_lock = new object();

    private volatile int m_currentNodeCount = 0;
    private volatile bool m_isRunning;

    public TerrainQuadTree(PlanetOrbitalCamera camera, int maxNodes = 10000, int minDepth = 6, int maxDepth = 21)
    {
        if (maxDepth > 23 || maxDepth < 1)
        {
            throw new ArgumentException("maxDepth must be greater than 1 and less than 23");
        }

        if (maxDepth < minDepth)
        {
            throw new ArgumentException("maxDepth must be greater than minDepth");
        }

        if (maxNodes <= 0)
        {
            throw new ArgumentException("maxNodes must be positive");
        }

        m_camera = camera ?? throw new ArgumentNullException(nameof(camera));
        m_maxNodes = maxNodes;
        m_minDepth = minDepth;
        m_maxDepth = maxDepth;
        m_isRunning = true;

        InitializeAltitudeThresholds();
        StartUpdateThread();
    }

    private void InitializeAltitudeThresholds()
    {
        double latitude = 0.0;
        double latitudeRadians = latitude * Math.PI / 180;
        m_splitThresholds = new double[m_maxDepth + 1];
        m_mergeThresholds = new double[m_maxDepth + 1];
        double baseRadius = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Math.Cos(latitudeRadians);
        double scalingFactor = 20.0;

        for (int zoom = 0; zoom <= m_maxDepth; zoom++)
        {
            double zoomScale = zoom >= 15 ? Math.Pow(0.85, zoom - 15) : 1.0;
            double threshold = baseRadius / Math.Pow(2, zoom);
            double baseThreshold = threshold * scalingFactor * zoomScale;

            m_splitThresholds[zoom] = baseThreshold;
            // TODO(Argyraspides, 15/02/2025) If not multiplied high enough, then we will oscillate between splitting/zooming all the time
            // If not multiplied small enough, then never merges. Ffs. find a balance quick.
            m_mergeThresholds[zoom] = baseThreshold * 3.5F;
        }
    }

    private void StartUpdateThread()
    {
        m_updateQuadTreeThread = new Thread(UpdateQuadTreeThreadFunction)
        {
            IsBackground = true, Name = "QuadTreeUpdateThread"
        };
        m_updateQuadTreeThread.Start();
    }

    private void UpdateQuadTreeThreadFunction()
    {
        while (m_isRunning)
        {
            try
            {
                lock (m_lock)
                {
                    if (m_rootNode != null)
                    {
                        UpdateQuadTree(m_rootNode);
                    }
                }

                Thread.Sleep(250);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in quadtree update thread: {ex}");
            }
        }
    }

    private void UpdateQuadTree(TerrainQuadTreeNode node)
    {
        if (node == null) return;

        CallDeferred("ShouldSplit", node);

        if (node.IsLoadedInScene && !node.ShouldSplit)
        {
            // Check if we need to cull nodes due to memory constraints
            if (m_currentNodeCount > m_maxNodes * m_maxNodesCleanupThreshold)
            {
                CullUnusedNodes(node);
            }

            return;
        }

        if (node.IsLoadedInScene && node.ShouldSplit)
        {
            CallDeferred("Split", node);
            return;
        }

        if (!node.IsLoadedInScene)
        {
            bool anyChildShouldMerge = false;
            for (int i = 0; i < 4; i++)
            {
                if (node.ChildNodes[i] != null && node.ChildNodes[i].IsLoadedInScene)
                {
                    CallDeferred("ShouldMerge", node.ChildNodes[i]);
                    anyChildShouldMerge |= node.ChildNodes[i].ShouldMerge;
                }
            }

            if (anyChildShouldMerge)
            {
                CallDeferred("Merge", node);
                return;
            }
        }

        if (!node.IsLoadedInScene)
        {
            for (int i = 0; i < 4; i++)
            {
                UpdateQuadTree(node.ChildNodes[i]);
            }
        }
    }

    /// <summary>
    /// Culls any unused nodes in the quadtree. The guaranteed way to know if any ancestors of a current
    /// parent node are visible or not is to check if the parent node itself is visible. If the parent node is visible
    /// (it is loaded in the scene), then this means the parent node satisfies the LoD requirements and thus none of its
    /// ancestors must be visible.
    /// </summary>
    /// <param name="node">The node where we attempt to cull all its ancestors. This node must currently be in use</param>
    private void CullUnusedNodes(TerrainQuadTreeNode node)
    {
        if (node == null) return;

        // If this node has no visible ancestors,
        if (!HasVisibleAncestors(node))
        {
            RemoveSubQuadTree(node);
        }
    }

    private bool HasVisibleAncestors(TerrainQuadTreeNode node)
    {
        if (node == null) return false;
        // If this node is loaded in the scene (thus visible), then it has satisfied the LoD requirements
        // and therefore none of its ancestors are visible.
        // If this node is NOT loaded in the scene, then it must have at least one visible ancestor.
        return !node.IsLoadedInScene;
    }

    public void InitializeQuadTree(int zoomLevel)
    {
        if (zoomLevel > m_maxDepth || zoomLevel < m_minDepth)
        {
            throw new ArgumentException("zoomLevel must be between min and max depth");
        }

        lock (m_lock)
        {
            // Clean up existing tree if any
            if (m_rootNode != null)
            {
                RemoveSubQuadTree(m_rootNode);
            }

            m_rootNode = new TerrainQuadTreeNode(new TerrainChunk(new MapTile(
                0.0F,
                0.0F,
                0
            )), 0);

            Queue<TerrainQuadTreeNode> q = new Queue<TerrainQuadTreeNode>();
            q.Enqueue(m_rootNode);

            for (int zLevel = 0; zLevel < zoomLevel; zLevel++)
            {
                // There are 4^z nodes in a quadtree at level z
                // 2^z * 2^z = 4^z
                int nodesInLevel = (1 << zLevel) * (1 << zLevel);
                for (int n = 0; n < nodesInLevel; n++)
                {
                    TerrainQuadTreeNode parentNode = q.Dequeue();
                    GenerateChildren(parentNode);
                    for (int i = 0; i < 4; i++)
                    {
                        q.Enqueue(parentNode.ChildNodes[i]);
                    }
                }
            }

            while (q.Count > 0)
            {
                TerrainQuadTreeNode node = q.Dequeue();
                InitializeTerrainQuadTreeNodeMesh(node);
            }
        }
    }

    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node cannot be null");
        }

        if (node.Depth >= m_maxDepth || !CameraInView(node))
        {
            node.ShouldSplit = false;
            return false;
        }

        float distToCam = node.Chunk.Position.DistanceTo(m_camera.Position);
        node.ShouldSplit = m_splitThresholds[node.Depth] > distToCam;
        return node.ShouldSplit;
    }


    private bool CameraInView(TerrainQuadTreeNode node)
    {
        double tileLatCoverage =
            MapUtils.TileToLatRange(node.Chunk.MapTile.LatitudeTileCoo, node.Chunk.MapTile.ZoomLevel);

        double tileLonCoverage =
            MapUtils.TileToLonRange(node.Chunk.MapTile.ZoomLevel);

        double minLat = m_camera.CurrentLat - m_camera.ApproxVisibleLatRadius - tileLatCoverage;
        double maxLat = m_camera.CurrentLat + m_camera.ApproxVisibleLatRadius + tileLatCoverage;

        double minLon = m_camera.CurrentLon - m_camera.ApproxVisibleLonRadius - tileLonCoverage;
        double maxLon = m_camera.CurrentLon + m_camera.ApproxVisibleLonRadius + tileLonCoverage;

        return
            node.Chunk.MapTile.Latitude > minLat &&
            node.Chunk.MapTile.Latitude < maxLat &&
            node.Chunk.MapTile.Longitude > minLon &&
            node.Chunk.MapTile.Longitude < maxLon;
    }

    private bool ShouldMerge(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node cannot be null");
        }

        if (node.Depth <= m_minDepth + 1 || !CameraInView(node))
        {
            node.ShouldMerge = false;
            return false;
        }

        float distToCam = node.Chunk.Position.DistanceTo(m_camera.Position);
        node.ShouldMerge = m_mergeThresholds[node.Depth] < distToCam;
        return node.ShouldMerge;
    }

    // Splits the terrain quad tree node into four children, and makes its parent invisible
    private void Split(TerrainQuadTreeNode node)
    {
        GenerateChildren(node);

        for (int i = 0; i < 4; i++)
        {
            InitializeTerrainQuadTreeNodeMesh(node.ChildNodes[i]);
        }

        node.Chunk.Visible = false;
        node.IsLoadedInScene = false;
        node.ShouldSplit = false;
        node.ShouldMerge = false;
    }

    // The input parameter is the parent quadtree whose children we wish to merge into it. Works by removing the children
    // from the scene tree/making them invisible, and then toggling itself to be visible or whatever
    private void Merge(TerrainQuadTreeNode parent)
    {
        if (parent == null) return;
        parent.Chunk.Visible = true;
        parent.IsLoadedInScene = true;
        for (int i = 0; i < 4; i++)
        {
            if (parent.ChildNodes[i] != null)
            {
                parent.ChildNodes[i].Chunk.Visible = false;
                parent.ChildNodes[i].IsLoadedInScene = false;
                parent.ChildNodes[i].ShouldSplit = false;
                parent.ChildNodes[i].ShouldMerge = false;
            }
        }
    }

    private void InitializeTerrainQuadTreeNodeMesh(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("Cannot initialize quad tree node mesh that is null");
        }

        var tile = node.Chunk.MapTile;

        if (tile == null)
        {
            throw new ArgumentException("Cannot initialize quad tree node mesh containing a null map tile");
        }

        // TODO(Argyraspides, 11/02/2025): Again, please, PLEASE make this WGS84 thing abstracted away too. TerrainQuadTree
        // shouldn't need to worry about this. Just generate the mesh and be done with it. Interfaces, interfaces,
        // interfaces!
        ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
            (float)tile.Latitude,
            (float)tile.Longitude,
            (float)tile.LatitudeRange,
            (float)tile.LongitudeRange
        );
        AddChild(node.Chunk);
        node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        node.Chunk.Load();
        node.Chunk.SetPositionAndSize();
        node.IsLoadedInScene = true;
        node.Chunk.Name = $"TerrainChunk_z{tile.ZoomLevel}_x{tile.LongitudeTileCoo}_y{tile.LatitudeTileCoo}";
    }

    void GenerateChildren(TerrainQuadTreeNode parentNode)
    {
        if (parentNode == null)
        {
            throw new ArgumentNullException("Cannot generate children of a terrain quad tree node that is null");
        }

        int parentLatTileCoo = parentNode.Chunk.MapTile.LatitudeTileCoo;
        int parentLonTileCoo = parentNode.Chunk.MapTile.LongitudeTileCoo;
        int zLevel = parentNode.Chunk.MapTile.ZoomLevel;
        // Generate all children
        for (int i = 0; i < 4; i++)
        {
            int childLatTileCoo = parentLatTileCoo * 2;
            int childLonTileCoo = parentLonTileCoo * 2;
            int childZoomLevel = zLevel + 1; // The children we are generating are one level down

            // (2 * row, 2 * col)         -- Top left child (i == 0)
            // (2 * row, 2 * col + 1)     -- Top right child (i == 1)
            // (2 * row + 1, 2 * col)     -- Bottom left child (i == 2)
            // (2 * row + 1, 2 * col + 1) -- Bottom right child (i == 3)
            childLatTileCoo += (i == 2 || i == 3) ? 1 : 0;
            childLonTileCoo += (i == 1 || i == 3) ? 1 : 0;

            // TODO(Argyraspides, 11/02/2025): Whether or not this is mercator should be abstracted away. Perhaps make
            // the MapTile have a constructor that can also take in lat/lon tile coordinates and then
            // automagically determine its fields. Then we can just pass in the lat/lon tile coordinates
            // and not worry about this conversion
            double childLat = MapUtils.MapTileToLatitude(childLatTileCoo, childZoomLevel);
            double childLon = MapUtils.MapTileToLongitude(childLonTileCoo, childZoomLevel);

            double childLatRange = MapUtils.TileToLatRange(childLatTileCoo, childZoomLevel);
            double childLonRange = MapUtils.TileToLonRange(childZoomLevel);

            double halfChildLatRange = childLatRange / 2;
            double halfChildLonRange = childLonRange / 2;

            double childCenterLat = childLat - halfChildLatRange;
            double childCenterLon = childLon + halfChildLonRange;

            parentNode.ChildNodes[i] = new TerrainQuadTreeNode(new TerrainChunk(new MapTile(
                (float)childCenterLat,
                (float)childCenterLon,
                childZoomLevel
            )), childZoomLevel);

            m_currentNodeCount++;
        }
    }

    private void RemoveQuadTreeNode(TerrainQuadTreeNode node)
    {
        if (node == null) return;

        // Remove from scene tree
        if (node.Chunk != null)
        {
            RemoveChild(node.Chunk);
            node.Chunk.QueueFree();
        }

        // Clear references
        node.Chunk = null;
        node.IsLoadedInScene = false;
        node.ShouldSplit = false;
        node.ShouldMerge = false;
        m_currentNodeCount--;
    }

    private void RemoveSubQuadTree(TerrainQuadTreeNode parent)
    {
        if (parent == null) return;
        for (int i = 0; i < 4; i++)
        {
            RemoveSubQuadTree(parent.ChildNodes[i]);
            parent.ChildNodes[i] = null;
        }
    }

    public override void _ExitTree()
    {
        m_isRunning = false;

        if (m_updateQuadTreeThread != null && m_updateQuadTreeThread.IsAlive)
        {
            m_updateQuadTreeThread.Join(1000);
        }

        base._ExitTree();
    }

    private sealed partial class TerrainQuadTreeNode : Node
    {
        public TerrainChunk Chunk { get; set; }
        public TerrainQuadTreeNode[] ChildNodes { get; set; }
        public bool IsLoadedInScene { get; set; }
        public bool ShouldSplit { get; set; }
        public bool ShouldMerge { get; set; }
        public int Depth { get; }

        public TerrainQuadTreeNode(TerrainChunk chunk, int depth)
        {
            Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            ChildNodes = new TerrainQuadTreeNode[4];
            IsLoadedInScene = false;
            ShouldSplit = false;
            ShouldMerge = false;
            Depth = depth;
        }
    }
}
