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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;


/**

TODO(Argyraspides, 16/02/2025) There is a lot to do here ...


Bug #3 (Unknown): I'm unsure whether the .NET garbage collector is actually doing anything, and running a memory profiler
to check heap allocations on all three generational heaps (and total heap) doesn't show them changing. Calling "queue_free"
only frees the C++ object in the Godot engine, but the actual TerrainQuadTreeNode reference remains in the C# world. I have
tried explicitly setting any and all references to null for TerrainQuadTreeNode when I know for sure Godot has finally
freed them (by using the IsInstanceValid() function), but I haven't actually observed the heap memory usage going down.
Then again, I didn't really watch the profiler for more than a couple minutes.

Bug #4: Happened only a couple times but sometimes I see a big black square as one of the map tiles. I think I caught one when
the Godot debugger said something like "Object reference not set to instance of object" when referring to either a TerrainChunk
or map tile, but I'm not sure

Potential Bug #5: See race condition TODO in the UpdateQuadTree() function

Bug #6: It seems we never cull nodes unless we start zooming out. This causes us to actually exceed our maximum node threshold
sometimes. We should be culling regardless of what happens so long as we don't cull what the user is currently viewing.

Bug #7: In the InitializeTerrainQuadTreeNodeMesh() function, sometimes the node chunk is null when we try and set the position and size,
even though we literally did a null check before entering the function. Idk why.

Bug #8: Tried zooming in way too far one time and it crashed. I don't know why. Error message involved trying to access an
object that was already disposed of

 */
public sealed partial class TerrainQuadTreeNode : Node
{
    public TerrainChunk Chunk { get; set; }
    public TerrainQuadTreeNode[] ChildNodes { get; set; }
    public bool IsLoadedInScene { get; set; }
    public int Depth { get; }

    public Vector3 Pos;

    public TerrainQuadTreeNode(TerrainChunk chunk, int depth)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        ChildNodes = new TerrainQuadTreeNode[4];
        Depth = depth;
        AddChild(Chunk);
    }
}

public partial class TerrainQuadTree : Node
{
    public PlanetOrbitalCamera m_camera;
    public TerrainQuadTreeNode m_rootNode;

    public long m_maxNodes;

    // Start cleanup of nodes when we hit 75% of max capacity
    public float m_maxNodesCleanupThreshold = 0.75F;

    public int m_minDepth;
    public int m_maxDepth;

    public double[] m_splitThresholds;
    public double[] m_mergeThresholds;

    public volatile int m_currentNodeCount = 0;
    public volatile bool m_isRunning;

    public volatile bool m_destructorActivated = false;

    private int m_maxQueueUpdatesPerFrame = 25;
    public ConcurrentQueue<TerrainQuadTreeNode> SplitQueueNodes = new ConcurrentQueue<TerrainQuadTreeNode>();
    public ConcurrentQueue<TerrainQuadTreeNode> MergeQueueNodes = new ConcurrentQueue<TerrainQuadTreeNode>();

    public Vector3 CameraPosition;
    TerrainQuadTreeUpdater m_quadTreeUpdater;

    private bool m_canUpdateQuadTree = false;

    [Signal]
    public delegate void QuadTreeUpdatedEventHandler();

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
        m_quadTreeUpdater = new TerrainQuadTreeUpdater(this);

        m_quadTreeUpdater.QuadTreeUpdatesDetermined += OnQuadTreeUpdatesDetermined;
    }

    void OnQuadTreeUpdatesDetermined()
    {
        m_canUpdateQuadTree = true;
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
            m_mergeThresholds[zoom] = baseThresholds[zoom] * 2.15F;
        }
    }

    public override void _Process(double delta)
    {
        CameraPosition = m_camera.Position;
        if (m_canUpdateQuadTree)
        {
            int maxDequeues = m_maxQueueUpdatesPerFrame;

            maxDequeues = m_maxQueueUpdatesPerFrame;
            while (SplitQueueNodes.Count > 0 && (maxDequeues--) > 0)
            {
                TerrainQuadTreeNode n;
                SplitQueueNodes.TryDequeue(out n);
                Split(n);
            }

            maxDequeues = m_maxQueueUpdatesPerFrame;
            while (MergeQueueNodes.Count > 0 && (maxDequeues--) > 0)
            {
                TerrainQuadTreeNode n;
                MergeQueueNodes.TryDequeue(out n);
                MergeChildren(n);
            }

            if (SplitQueueNodes.Count == 0 && MergeQueueNodes.Count == 0)
            {
                m_canUpdateQuadTree = false;
                EmitSignal("QuadTreeUpdated");
            }
        }
    }


    public void InitializeQuadTree(int zoomLevel)
    {
        if (zoomLevel > m_maxDepth || zoomLevel < m_minDepth)
        {
            throw new ArgumentException("zoomLevel must be between min and max depth");
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
                for (int i = 0; i < parentNode.ChildNodes.Length; i++)
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

    // Splits the terrain quad tree node into four children, and makes its parent invisible
    private void Split(TerrainQuadTreeNode node)
    {
        GenerateChildren(node);

        for (int i = 0; i < node.ChildNodes.Length; i++)
        {
            if (node.ChildNodes[i] != null)
            {
                InitializeTerrainQuadTreeNodeMesh(node.ChildNodes[i]);
            }
        }

        node.Chunk.Visible = false;
        node.IsLoadedInScene = false;
    }

    // The input parameter is the parent quadtree whose children we wish to merge into it. Works by removing the children
    // from the scene tree/making them invisible, and then toggling itself to be visible or whatever
    private void MergeChildren(TerrainQuadTreeNode parent)
    {
        if (!IsInstanceValid(parent))
        {
            return;
        }

        parent.Chunk.Visible = true;
        parent.IsLoadedInScene = true;

        for (int i = 0; i < parent.ChildNodes.Length; i++)
        {
            if (IsInstanceValid(parent.ChildNodes[i]))
            {
                parent.ChildNodes[i].Chunk.Visible = false;
                parent.ChildNodes[i].IsLoadedInScene = false;
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
        node.Chunk?.Load();
        AddChild(node);
        node.Chunk?.SetPositionAndSize();
        node.Pos = node.Chunk.Position;
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


    public override void _ExitTree()
    {
        m_isRunning = false;

        if (m_quadTreeUpdater.m_updateQuadTreeThread != null && m_quadTreeUpdater.m_updateQuadTreeThread.IsAlive)
        {
            m_quadTreeUpdater.m_updateQuadTreeThread.Join(1000);
        }

        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            m_destructorActivated = true;
        }

        if (!m_destructorActivated && what == NotificationChildOrderChanged)
        {
            m_currentNodeCount = GetTree().GetNodeCount();
        }
    }
}

public partial class TerrainQuadTreeUpdater : Node
{
    public Thread m_updateQuadTreeThread;

    private TerrainQuadTree m_terrainQuadTree;
    private int m_quadTreeUpdateInterval = 250;
    private TerrainQuadTreeNode m_rootNode;
    private bool m_isRunning = false;

    [Signal]
    public delegate void QuadTreeUpdatesDeterminedEventHandler();

    private volatile bool m_canPerformSearch = true;

    public void OnQuadTreeUpdated()
    {
        m_canPerformSearch = true;
    }

    public TerrainQuadTreeUpdater(TerrainQuadTree terrainQuadTree)
    {
        m_terrainQuadTree = terrainQuadTree;
        terrainQuadTree.QuadTreeUpdated += OnQuadTreeUpdated;
        StartUpdateThread();
    }

    private void StartUpdateThread()
    {
        m_updateQuadTreeThread = new Thread(UpdateQuadTreeThreadFunction)
        {
            IsBackground = true, Name = "QuadTreeUpdateThread"
        };
        m_updateQuadTreeThread.Start();
        m_isRunning = true;
    }

    private void UpdateQuadTreeThreadFunction()
    {
        while (m_isRunning)
        {
            try
            {
                if (m_terrainQuadTree.m_rootNode != null && m_canPerformSearch)
                {
                    UpdateQuadTree(m_terrainQuadTree.m_rootNode);
                }

                if (m_canPerformSearch)
                {
                    EmitSignal(SignalName.QuadTreeUpdatesDetermined);
                    m_canPerformSearch = false;
                }

                Thread.Sleep(m_quadTreeUpdateInterval);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in quadtree update thread: {ex}");
            }
        }
    }

    private void UpdateQuadTree(TerrainQuadTreeNode node)
    {
        if (!Godot.GodotObject.IsInstanceValid(node))
        {
            node = null;
            return;
        }

        if (node == null || node.IsQueuedForDeletion())
        {
            return;
        }

        if (node.IsLoadedInScene)
        {
            if (m_terrainQuadTree.m_currentNodeCount >
                m_terrainQuadTree.m_maxNodes * m_terrainQuadTree.m_maxNodesCleanupThreshold)
            {
                CullUnusedNodes(node);
            }
        }

        if (node.IsLoadedInScene && ShouldSplit(node))
        {
            m_terrainQuadTree.SplitQueueNodes.Enqueue(node);
            return;
        }

        if (ShouldChildrenMerge(node))
        {
            m_terrainQuadTree.MergeQueueNodes.Enqueue(node);
            return;
        }

        if (!node.IsLoadedInScene)
        {
            for (int i = 0; i < node.ChildNodes.Length; i++)
            {
                UpdateQuadTree(node.ChildNodes[i]);
            }
        }
    }

    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        if (!Godot.GodotObject.IsInstanceValid(node))
        {
            throw new ArgumentNullException("node cannot be null");
        }

        if (node.Depth >= m_terrainQuadTree.m_maxDepth)
        {
            return false;
        }

        float distToCam = node.Pos.DistanceTo(m_terrainQuadTree.CameraPosition);
        return m_terrainQuadTree.m_splitThresholds[node.Depth] > distToCam;
    }

    private bool ShouldMerge(TerrainQuadTreeNode node)
    {
        if (!Godot.GodotObject.IsInstanceValid(node))
        {
            return false;
        }

        if (node.Depth <= m_terrainQuadTree.m_minDepth + 1)
        {
            return false;
        }

        float distToCam = node.Pos.DistanceTo(m_terrainQuadTree.CameraPosition);
        return m_terrainQuadTree.m_mergeThresholds[node.Depth] < distToCam;
    }

    private bool ShouldChildrenMerge(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node cannot be null");
        }

        bool shouldAllChildrenMerge = false;
        for (int i = 0; i < node.ChildNodes.Length; i++)
        {
            shouldAllChildrenMerge |= ShouldMerge(node.ChildNodes[i]);
        }

        return shouldAllChildrenMerge;
    }

    private void RemoveQuadTreeNode(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            return;
        }

        if (IsInstanceValid(node))
        {
            // We are telling Godot to queue the object for deletion from a different thread,
            // so we must use CallDeferred
            node.CallDeferred("queue_free");
        }
    }

    private void RemoveSubQuadTreeThreadSafe(TerrainQuadTreeNode parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.ChildNodes.Length; i++)
        {
            RemoveSubQuadTreeThreadSafe(parent.ChildNodes[i]);
            RemoveQuadTreeNode(parent.ChildNodes[i]);
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
        if (parentNode == null)
        {
            return;
        }

        // If this node has no visible ancestors
        if (parentNode.IsLoadedInScene)
        {
            RemoveSubQuadTreeThreadSafe(parentNode);
        }
    }
}
