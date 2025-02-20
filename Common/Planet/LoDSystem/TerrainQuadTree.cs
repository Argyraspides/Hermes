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

namespace Hermes.Common.Planet.LoDSystem;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The TerrainQuadTree class is a custom quadtree implementation meant for any generic LoD requirement, and
/// is meant for use in 3D space.
///
/// It works by taking in a Camera3D instance, and based on the camera's FOV and distance from each
/// TerrainChunk in the world, will determine whether or not chunks need to be split/merged. This is meant to be
/// used within Planet objects, where the TerrainQuadTree will act as the central manager for the LoD system of
/// the TerrainChunk's that represent the planet's surface.
///
/// TerrainQuadTree depends on the class TerrainQuadTreeUpdater, which performs a tree traversal on a separate thread
/// in order to queue up TerrainQuadTreeNode's that should be split/merged. This TerrainQuadTreeUpdater also culls unused
/// nodes on a different thread. Nodes that need to be split/merged are queued up in TerrainQuadTree and processed during
/// the game loop over many frames to reduce in-game lag.
/// </summary>
public sealed partial class TerrainQuadTree : Node
{
    #region Constants & Configuration

    // If we hit x% of the maximum allowed amount of nodes, we will begin culling unused nodes in the quadtree
    public float MaxNodesCleanupThresholdPercent = 0.90F;

    // Maximum amount of split and merge operations allowed per frame in the scene tree.
    // This is to spread the workload of splitting/merging over multiple frames
    public const int MaxQueueUpdatesPerFrame = 2;

    // To prevent hysterisis and oscillation between merging/splitting at fine boundaries, we multiply the merge
    // thresholds to be greater than the split thresholds
    public const float MergeThresholdFactor = 1.5F;

    // Hard limit of allowed depth of the quadtree
    public const int MaxDepthLimit = 23;

    // Hard limit of minimum allowed depth of the quadtree
    public const int MinDepthLimit = 1;

    private readonly double[] m_baseAltitudeThresholds = new double[]
    {
        156000.0f, 78000.0f, 39000.0f, 19500.0f, 9750.0f, 4875.0f, 2437.5f, 1218.75f, 609.375f, 304.6875f, 152.34f,
        76.17f, 38.08f, 19.04f, 9.52f, 4.76f, 2.38f, 1.2f, 0.6f, 0.35f
    };

    #endregion Constants & Configuration

    #region Dependencies & State

    public readonly PlanetOrbitalCamera m_camera;
    public readonly TerrainQuadTreeUpdater m_quadTreeUpdater;

    // Current minimum and maximum depths allowed
    public readonly int m_maxDepth;
    public readonly int m_minDepth;

    // Maximum allowed nodes in the scene tree
    public long m_maxNodes;

    // Thresholds below/above which we will split/merge nodes respectively. Each index represents a zoom level,
    // the value at the index represents the threshold in kilometers
    public double[] m_splitThresholds;
    public double[] m_mergeThresholds;

    // Current amount of nodes in the scene tree (in total -- not just the quadtree)
    public volatile int m_currentNodeCount = 0;

    // True if the TerrainQuadTree is about to be destroyed. Used as we don't want to update our current node count
    // when the game is closing and nodes in the scene tree may be invalid
    public volatile bool m_destructorActivated = false;

    // We only process split/merge operations when the TerrainQuadTreeUpdater is finished determining
    // which nodes should be split/merged at a particular point in time. This is set to true upon a signal
    // emmitted by TerrainQuadTreeUpdater when it is done with a tree traversal iteration
    public bool m_canUpdateQuadTree = false;

    public int m_currentCameraZoomLevel;

    // Mutex to access the root nodes
    public object rootNodeLock = new object();

    // List of root nodes. We are allowed a minimum zoom level of above 1, thus all nodes at zoom level 'z',
    // where 1 < z < m_maxDepth are unnecessary to keep in memory. This is a list of all the root nodes at the minimum
    // depth where we can start a quadtree traversal
    public List<TerrainQuadTreeNode> RootNodes { get; private set; }

    // Reading inherent properties of nodes is not thread-safe in Godot. Here we make a custom Vector3
    // which is a copy of the camera's position updated from the TerrainQuadTree thread, so that it can
    // be accessed by the TerrainQuadTreeUpdater thread safely
    public Vector3 CameraPosition { get; private set; }

    // Queue of nodes that should be split/merged as determined by the TerrainQuadTreeUpdater
    public ConcurrentQueue<TerrainQuadTreeNode> SplitQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();
    public ConcurrentQueue<TerrainQuadTreeNode> MergeQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();

    #endregion Dependencies & State

    #region Signals

    // Signal emitted by TerrainQuadTree to the TerrainQuadTreeUpdater when we have finished splitting/merging
    // all nodes in the queue, so that TerrainQuadTreeUpdater can safely run the next tree traversal
    [Signal]
    public delegate void QuadTreeUpdatedEventHandler();

    #endregion Signals

    #region Constructor & Initialization

    public TerrainQuadTree(PlanetOrbitalCamera camera, int maxNodes = 17500, int minDepth = 6, int maxDepth = 20)
    {
        ValidateConstructorArguments(maxDepth, minDepth, maxNodes);

        m_camera = camera ?? throw new ArgumentNullException(nameof(camera));
        m_maxNodes = maxNodes;
        m_minDepth = minDepth;
        m_maxDepth = maxDepth;

        InitializeAltitudeThresholds();
        m_quadTreeUpdater = new TerrainQuadTreeUpdater(this);
        m_quadTreeUpdater.QuadTreeUpdatesDetermined += OnQuadTreeUpdatesDetermined;
    }

    private void ValidateConstructorArguments(int maxDepth, int minDepth, int maxNodes)
    {
        if (maxDepth > MaxDepthLimit || maxDepth < MinDepthLimit)
        {
            throw new ArgumentException($"maxDepth must be between {MinDepthLimit} and {MaxDepthLimit}");
        }

        if (maxDepth < minDepth)
        {
            throw new ArgumentException("maxDepth must be greater than minDepth");
        }

        if (maxNodes <= 0)
        {
            throw new ArgumentException("maxNodes must be positive");
        }
    }

    private void InitializeAltitudeThresholds()
    {
        m_splitThresholds = new double[m_maxDepth + 1];
        m_mergeThresholds = new double[m_maxDepth + 1];

        for (int zoom = 0; zoom < m_maxDepth; zoom++)
        {
            m_splitThresholds[zoom] = m_baseAltitudeThresholds[zoom];
            m_mergeThresholds[zoom] = m_baseAltitudeThresholds[zoom] * MergeThresholdFactor;
        }
    }

    #endregion Constructor & Initialization

    #region Godot Lifecycle

    public override void _Process(double delta)
    {
        CameraPosition = m_camera.Position;
        if (m_canUpdateQuadTree)
        {
            ProcessSplitQueue();
            ProcessMergeQueue();

            if (SplitQueueNodes.IsEmpty && MergeQueueNodes.IsEmpty)
            {
                m_canUpdateQuadTree = false;
                EmitSignal(SignalName.QuadTreeUpdated);
            }
        }
    }

    private void ProcessSplitQueue()
    {
        int dequeuesProcessed = 0;
        while (SplitQueueNodes.TryDequeue(out TerrainQuadTreeNode node) &&
               dequeuesProcessed++ < MaxQueueUpdatesPerFrame)
        {
            SplitNode(node);
        }
    }

    private void ProcessMergeQueue()
    {
        int dequeuesProcessed = 0;
        while (MergeQueueNodes.TryDequeue(out TerrainQuadTreeNode node) &&
               dequeuesProcessed++ < MaxQueueUpdatesPerFrame)
        {
            MergeNodeChildren(node);
        }
    }

    public override void _ExitTree()
    {
        m_quadTreeUpdater.StopUpdateThread();
        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            m_destructorActivated = true;
        }
        else if (!m_destructorActivated && what == NotificationChildOrderChanged)
        {
            m_currentNodeCount = GetTree().GetNodeCount();
        }
    }

    #endregion Godot Lifecycle

    #region QuadTree Initialization & Manipulation

    public void InitializeQuadTree(int zoomLevel)
    {
        ValidateZoomLevel(zoomLevel);

        lock (rootNodeLock)
        {
            Queue<TerrainQuadTreeNode> nodeQueue = new Queue<TerrainQuadTreeNode>();
            RootNodes = new List<TerrainQuadTreeNode>();

            int nodesPerSide = (1 << m_minDepth); // 2^z
            int nodesInLevel = nodesPerSide * nodesPerSide; // 4^z
            for (int i = 0; i < nodesInLevel; i++)
            {
                int latTileCoo = i / nodesPerSide;
                int lonTileCoo = i % nodesPerSide;
                TerrainQuadTreeNode n = CreateNode(latTileCoo, lonTileCoo, m_minDepth);
                RootNodes.Add(n);
                nodeQueue.Enqueue(RootNodes[i]);
            }

            for (int zLevel = m_minDepth; zLevel < zoomLevel; zLevel++)
            {
                nodesInLevel = 1 << (2 * zLevel); // 4^z
                for (int n = 0; n < nodesInLevel; n++)
                {
                    TerrainQuadTreeNode parentNode = nodeQueue.Dequeue();
                    GenerateChildNodes(parentNode);
                    EnqueueChildren(nodeQueue, parentNode);
                }
            }

            InitializeMeshesInQueue(nodeQueue);
        }
    }

    private void EnqueueChildren(Queue<TerrainQuadTreeNode> queue, TerrainQuadTreeNode parentNode)
    {
        foreach (var childNode in parentNode.ChildNodes)
        {
            queue.Enqueue(childNode);
        }
    }

    private void InitializeMeshesInQueue(Queue<TerrainQuadTreeNode> queue)
    {
        while (queue.Count > 0)
        {
            TerrainQuadTreeNode node = queue.Dequeue();
            InitializeTerrainNodeMesh(node);
        }
    }

    private void ValidateZoomLevel(int zoomLevel)
    {
        if (zoomLevel > m_maxDepth || zoomLevel < m_minDepth)
        {
            throw new ArgumentException($"zoomLevel must be between {m_minDepth} and {m_maxDepth}");
        }
    }

    /// <summary>
    /// Splits the quad tree nodes by initializing its children. If its children already exists, simply
    /// toggles their visibility on and itself off. This function is async as generating child nodes if they
    /// do not exist require a backend API call to retrieve map tile information, or retrieving a map tile from cache
    /// </summary>
    /// <param name="node">Node to be split</param>
    /// <exception cref="ArgumentNullException">Thrown if the TerrainQuadTreeNode is not valid</exception>
    private async void SplitNode(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node))
        {
            throw new ArgumentNullException("Attempting to split an invalid terrain quad tree node");
        }

        if (!node.HasChildren())
        {
            GenerateChildNodes(node);
            foreach (var childNode in node.ChildNodes)
            {
                await InitializeTerrainNodeMesh(childNode);
            }
        }

        foreach (var childNode in node.ChildNodes)
        {
            childNode.IsLoadedInScene = true;
            childNode.Chunk.Visible = true;
        }

        node.Chunk.Visible = false;
        node.IsLoadedInScene = false;
    }

    private void MergeNodeChildren(TerrainQuadTreeNode parent)
    {
        if (!GodotUtils.IsValid(parent)) return;

        parent.Chunk.Visible = true;
        parent.IsLoadedInScene = true;

        foreach (var childNode in parent.ChildNodes)
        {
            if (GodotUtils.IsValid(childNode))
            {
                childNode.Chunk.Visible = false;
                childNode.IsLoadedInScene = false;
            }
        }
    }

    private async Task InitializeTerrainNodeMesh(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node) || !GodotUtils.IsValid(node.Chunk))
        {
            throw new ArgumentNullException("Trying to initialize terrain node mesh that is null");
        }

        ValidateTerrainNodeForMeshInitialization(node);

        if (!GodotUtils.IsValid(node.Chunk.MeshInstance))
        {
            ArrayMesh meshSegment = await GenerateMeshForNode(node);
            node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        }

        node.Chunk.Load();
        AddChild(node);
        node.IsLoadedInScene = true;
        node.Chunk.SetPositionAndSize();
        node.SetPosition(node.Chunk.Position);
        node.Chunk.Name = GenerateChunkName(node);
    }

    private async Task<ArrayMesh> GenerateMeshForNode(TerrainQuadTreeNode node)
    {
        return await Task.Run(() =>
        {
            ArrayMesh meshSegment =
                // TODO(Argyraspides, 19/02/2025): Please please please abstract this away. Do not hardcode the mesh type we are using.
                // WGS84 only really applies to the Earth. This won't work for other planets.
                WGS84EllipsoidMeshGenerator
                    .CreateEllipsoidMeshSegment(
                        (float)node.Chunk.MapTile.Latitude,
                        (float)node.Chunk.MapTile.Longitude,
                        (float)node.Chunk.MapTile.LatitudeRange,
                        (float)node.Chunk.MapTile.LongitudeRange
                    );
            return meshSegment;
        });
    }

    private string GenerateChunkName(TerrainQuadTreeNode node)
    {
        return
            $"TerrainChunkm_z{node.Chunk.MapTile.ZoomLevel}m_x{node.Chunk.MapTile.LongitudeTileCoo}m_y{node.Chunk.MapTile.LatitudeTileCoo}";
    }

    private void ValidateTerrainNodeForMeshInitialization(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node) || node == null)
            throw new ArgumentNullException(nameof(node), "Cannot initialize mesh for a null node.");
        if (!GodotUtils.IsValid(node.Chunk) || node.Chunk == null)
            throw new ArgumentNullException(nameof(node.Chunk), "Node's chunk is null.");
        if (node.Chunk.MapTile == null)
            throw new ArgumentNullException(nameof(node.Chunk.MapTile), "Chunk's MapTile is null.");
    }

    private void GenerateChildNodes(TerrainQuadTreeNode parentNode)
    {
        if (parentNode == null)
            throw new ArgumentNullException(nameof(parentNode), "Cannot generate children for a null node.");

        int parentLatTileCoo = parentNode.Chunk.MapTile.LatitudeTileCoo;
        int parentLonTileCoo = parentNode.Chunk.MapTile.LongitudeTileCoo;
        int childZoomLevel = parentNode.Chunk.MapTile.ZoomLevel + 1;

        for (int i = 0; i < 4; i++)
        {
            (int childLatTileCoo, int childLonTileCoo) =
                CalculateChildTileCoordinates(parentLatTileCoo, parentLonTileCoo, i);
            parentNode.ChildNodes[i] = CreateNode(childLatTileCoo, childLonTileCoo, childZoomLevel);
        }
    }

    private (int childLatTileCoo, int childLonTileCoo) CalculateChildTileCoordinates(int parentLatTileCoo,
        int parentLonTileCoo, int childIndex)
    {
        int childLatTileCoo = parentLatTileCoo * 2 + ((childIndex == 2 || childIndex == 3) ? 1 : 0);
        int childLonTileCoo = parentLonTileCoo * 2 + ((childIndex == 1 || childIndex == 3) ? 1 : 0);
        return (childLatTileCoo, childLonTileCoo);
    }

    private TerrainQuadTreeNode CreateNode(int latTileCoo, int lonTileCoo, int zoomLevel)
    {
        // TODO(Argyraspides, 19/02/2025): Abstract away Mercator/WGS84 specifics from TerrainQuadTree
        double childLat = MapUtils.TileCoordinateToLatitude(latTileCoo, zoomLevel);
        double childLon = MapUtils.TileCoordinateToLongitude(lonTileCoo, zoomLevel);
        double childLatRange = MapUtils.TileToLatRange(latTileCoo, zoomLevel);
        double childLonRange = MapUtils.TileToLonRange(zoomLevel);
        double halfChildLatRange = childLatRange / 2;
        double halfChildLonRange = childLonRange / 2;
        double childCenterLat = childLat - halfChildLatRange;
        double childCenterLon = childLon + halfChildLonRange;

        var childChunk = new TerrainChunk(new MapTile((float)childCenterLat, (float)childCenterLon, zoomLevel));
        return new TerrainQuadTreeNode(childChunk, zoomLevel);
    }

    private void OnQuadTreeUpdatesDetermined()
    {
        m_canUpdateQuadTree = true;
    }

    #endregion QuadTree Initialization & Manipulation
}

/**

TODO(Argyraspides, 16/02/2025) There is a lot to do here ...

Bug #3 (Unknown): I'm unsure whether the .NET garbage collector is actually doing anything, and running a memory profiler
to check heap allocations on all three generational heaps (and total heap) doesn't show them changing. Calling "queuem_free"
only frees the C++ object in the Godot engine, but the actual TerrainQuadTreeNode reference remains in the C# world. I have
tried explicitly setting any and all references to null for TerrainQuadTreeNode when I know for sure Godot has finally
freed them (by using the GodotUtils.IsValid() function), but I haven't actually observed the heap memory usage going down.
Then again, I didn't really watch the profiler for more than a couple minutes.

Bug #4: Happened only a couple times but sometimes I see a big black square as one of the map tiles.

Bug #6: It seems we never cull nodes unless we start zooming out. This causes us to actually exceed our maximum node threshold
sometimes. We should be culling regardless of what happens so long as we don't cull what the user is currently viewing.

*/
