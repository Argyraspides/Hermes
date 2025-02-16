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

    public TerrainQuadTree(PlanetOrbitalCamera camera, int maxNodes = 15000, int minDepth = 6, int maxDepth = 20)
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
        double[] baseThresholds = new double[]
        {
            156000.0f, // Level 0  - Full Earth view
            78000.0f, // Level 1  - Continent level
            39000.0f, // Level 2  - Large country
            19500.0f, // Level 3  - Small country
            9750.0f, // Level 4  - Large state/province
            4875.0f, // Level 5  - Small state/province
            2437.5f, // Level 6  - Large metropolitan area
            1218.75f, // Level 7  - City level
            609.375f, // Level 8  - District level
            304.6875f, // Level 9  - Neighborhood
            152.34f, // Level 10 - Street level
            76.17f, // Level 11 - Building level
            38.08f, // Level 12 - Building detail
            19.04f, // Level 13 - Close building view
            9.52f, // Level 14 - Very close building
            4.76f, // Level 15 - Ground level
            2.38f, // Level 16 - Detailed ground
            1.2f, // Level 17 - High detail
            0.6f, // Level 18 - Very high detail
            0.35f // Level 19 - Maximum detail
        };
        m_splitThresholds = new double[m_maxDepth + 1];
        m_mergeThresholds = new double[m_maxDepth + 1];
        for (int zoom = 0; zoom < m_maxDepth; zoom++)
        {
            m_splitThresholds[zoom] = baseThresholds[zoom];
            m_mergeThresholds[zoom] = baseThresholds[zoom] * 7F;
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
        if (node == null || !IsInstanceValid(node) || node.IsQueuedForDeletion())
        {
            return;
        }

        CallDeferred("ShouldSplit", node);

        if (!HasVisibleAncestors(node))
        {
            if (m_currentNodeCount > m_maxNodes * m_maxNodesCleanupThreshold)
            {
                CullUnusedNodes(node);
            }
        }

        if (node.IsLoadedInScene && node.ShouldSplit)
        {
            CallDeferred("Split", node);
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
    /// <param name="parentNode">The node where we attempt to cull all its ancestors. This node must currently be in use</param>
    private void CullUnusedNodes(TerrainQuadTreeNode parentNode)
    {
        if (parentNode == null) return;

        // If this node has no visible ancestors,
        if (!HasVisibleAncestors(parentNode))
        {
            RemoveSubQuadTreeThreadSafe(parentNode);
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
                RemoveSubQuadTreeThreadSafe(m_rootNode);
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

        m_currentNodeCount = GetTree().GetNodeCount();
        GD.Print($"Initial node count: {m_currentNodeCount}");
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
        return true;
        double tileLatCoverage =
            MapUtils.TileToLatRange(node.Chunk.MapTile.LatitudeTileCoo, node.Chunk.MapTile.ZoomLevel) * 5;

        double tileLonCoverage =
            MapUtils.TileToLonRange(node.Chunk.MapTile.ZoomLevel) * 5;

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
            if (node.ChildNodes[i] != null)
            {
                InitializeTerrainQuadTreeNodeMesh(node.ChildNodes[i]);
            }
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

        if (node.Chunk == null)
        {
            throw new ArgumentNullException("Cannot initialize quad tree node mesh containing a null chunk");
        }

        if (node.Chunk.MapTile == null)
        {
            throw new ArgumentNullException("Cannot initialize quad tree node mesh containing a null map tile");
        }

        // TODO(Argyraspides, 11/02/2025): Again, please, PLEASE make this WGS84 thing abstracted away too. TerrainQuadTree
        // shouldn't need to worry about this. Just generate the mesh and be done with it. Interfaces, interfaces,
        // interfaces!
        ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
            (float)node.Chunk.MapTile.Latitude,
            (float)node.Chunk.MapTile.Longitude,
            (float)node.Chunk.MapTile.LatitudeRange,
            (float)node.Chunk.MapTile.LongitudeRange
        );
        node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        node.Chunk.Load();
        AddChild(node);
        node.Chunk.SetPositionAndSize();
        node.IsLoadedInScene = true;
        node.Chunk.Name =
            $"TerrainChunk_z{node.Chunk.MapTile.ZoomLevel}_x{node.Chunk.MapTile.LongitudeTileCoo}_y{node.Chunk.MapTile.LatitudeTileCoo}";
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
        }
    }

    private void RemoveQuadTreeNodeThreadSafe(TerrainQuadTreeNode node)
    {
        if (node == null) return;
        lock (m_lock)
        {
            if (IsInstanceValid(node))
            {
                node.CallDeferred("queue_free");
            }
        }
    }

    private void RemoveSubQuadTreeThreadSafe(TerrainQuadTreeNode parent)
    {
        if (parent == null) return;
        for (int i = 0; i < 4; i++)
        {
            RemoveSubQuadTreeThreadSafe(parent.ChildNodes[i]);
            RemoveQuadTreeNodeThreadSafe(parent.ChildNodes[i]);
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
            AddChild(Chunk);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationChildOrderChanged)
        {
            int currentCount = GetTree().GetNodeCount();
            if (currentCount != m_currentNodeCount)
            {
                m_currentNodeCount = currentCount;
                GD.Print($"Node count changed: {m_currentNodeCount}");
            }
        }
    }
}
