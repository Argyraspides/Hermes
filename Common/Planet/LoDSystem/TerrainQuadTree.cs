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


using Hermes.Common.Map.Types;
using Hermes.Common.Map.Utils;
using Hermes.Common.Meshes.MeshGenerators;
using Hermes.Universe.SolarSystem;

namespace Hermes.Common.Planet.LoDSystem;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using Hermes.Common.GodotUtils;

/// <summary>
/// The TerrainQuadTree class is a custom quadtree implementation meant for any generic LoD requirement, and
/// is meant for use in 3D space.
///
/// It works by taking in a Camera3D instance, and based on the camera's FOV and distance from each
/// TerrainChunk in the world, will determine whether chunks need to be split/merged. This is meant to be
/// used within Planet objects, where the TerrainQuadTree will act as the central manager for the LoD system of
/// the TerrainChunk's that represent the planet's surface.
///
/// TerrainQuadTree depends on the class TerrainQuadTreeUpdater, which performs a tree traversal on a separate thread
/// in order to queue up TerrainQuadTreeNode's that should be split/merged. This TerrainQuadTreeUpdater also culls unused
/// nodes on a different thread. Nodes that need to be split/merged are queued up in TerrainQuadTree and processed during
/// the game loop over many frames to reduce in-game lag.
///
/// When the TerrainQuadTree is finished merging/splitting, it will signal to the TerrainQuadTreeUpdater that it has completed
/// this work. The TerrainQuadTreeUpdater will then attempt to cull any unused nodes. When it is finished doing so, it will
/// again traverse the quadtree to determine which nodes should be split/merged, signal to the TerrainQuadTree when it is finished,
/// and the cycle repeats.
///
/// </summary>
public sealed partial class TerrainQuadTree : Node3D
{

    // If we hit x% of the maximum allowed amount of nodes, we will begin culling unused nodes in the quadtree
    public float MaxNodesCleanupThresholdPercent = 0.90F;

    // Maximum amount of split and merge operations allowed per frame in the scene tree.
    // This is to spread the workload of splitting/merging over multiple frames
    public const int MaxQueueUpdatesPerFrame = 2;

    // To prevent hysterisis and oscillation between merging/splitting at fine boundaries, we multiply the merge
    // thresholds to be greater than the split thresholds
    public const float MergeThresholdFactor = 1.15F;

    // Hard limit of allowed depth of the quadtree
    public const int MAX_DEPTH_LIMIT = 23;

    // Hard limit of minimum allowed depth of the quadtree
    public const int MIN_DEPTH_LIMIT = 1;

    // To prevent seams, we keep only the parent chunk of the current deepest visible.
    // This is the offset used to determine the draw order (children should be drawn after parents)
    public const float CHUNK_SORT_OFFSET = 10.0f;

    // TODO(Argyrsapides, 22/02/2025): Make this a configurable curve or something
    private readonly double[] m_baseAltitudeThresholds = new double[]
    {
        156000.0f, 78000.0f, 39000.0f, 19500.0f, 9750.0f, 4875.0f, 2437.5f, 1218.75f, 609.375f, 304.6875f, 152.34f,
        76.17f, 38.08f, 19.04f, 9.52f, 4.76f, 2.38f, 1.2f, 0.6f, 0.35f
    };

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
    public double CameraAltitude { get; private set; }
    public double CameraFov { get; private set; }
    public int CameraZoomLevel { get; private set; }
    public double CameraLat { get; private set; }
    public double CameraLon { get; private set; }

    // Queue of nodes that should be split/merged as determined by the TerrainQuadTreeUpdater
    public ConcurrentQueue<TerrainQuadTreeNode> SplitQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();
    public ConcurrentQueue<TerrainQuadTreeNode> MergeQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();

    // Signal emitted by TerrainQuadTree to the TerrainQuadTreeUpdater when we have finished splitting/merging
    // all nodes in the queue, so that TerrainQuadTreeUpdater can safely run the next tree traversal
    [Signal]
    public delegate void QuadTreeUpdatedEventHandler();

    public TerrainQuadTree(PlanetOrbitalCamera camera, int maxNodes = 7500, int minDepth = 6, int maxDepth = 20)
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
        if (maxDepth > MAX_DEPTH_LIMIT || maxDepth < MIN_DEPTH_LIMIT)
        {
            throw new ArgumentException($"maxDepth must be between {MIN_DEPTH_LIMIT} and {MAX_DEPTH_LIMIT}");
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
        m_mergeThresholds = new double[m_maxDepth + 2];

        for (int zoom = 0; zoom < m_maxDepth; zoom++)
        {
            m_splitThresholds[zoom] = m_baseAltitudeThresholds[zoom];
        }

        for (int zoom = 1; zoom < m_maxDepth; zoom++)
        {
            m_mergeThresholds[zoom] =
                m_splitThresholds[zoom - 1] *
                MergeThresholdFactor;
        }
    }

    public override void _Process(double delta)
    {
        CameraPosition = m_camera.Position;
        CameraAltitude = m_camera.CurrentAltitude;
        CameraFov = m_camera.Fov;
        CameraLat = m_camera.Lat;
        CameraLon = m_camera.Lon;

        for (int i = m_baseAltitudeThresholds.Length - 1; i > 1; i--)
        {
            if (m_baseAltitudeThresholds[i] < m_camera.CurrentAltitude  &&
                m_baseAltitudeThresholds[i - 1] > m_camera.CurrentAltitude )
            {
                m_camera.CurrentZoomLevel = i;
                CameraZoomLevel = i;
                break;
            }
        }

        if (m_canUpdateQuadTree)
        {
            lock (rootNodeLock)
            {
                ProcessSplitQueue();
                ProcessMergeQueue();
            }

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
        // We don't want to attempt to GetTree().GetNodeCount() when the scene tree (or at the very least,
        // the TerrainQuadTree) is about to be deleted.
        else if (!m_destructorActivated && what == NotificationChildOrderChanged)
        {
            m_currentNodeCount = GetTree().GetNodeCount();
        }
    }

    /// <summary>
    /// Initializes the quadtree at the specified zoom level. Note that if the current minimum zoom level
    /// is greater than one, then all nodes below this zoom level will never exist in the scene tree
    /// </summary>
    /// <param name="zoomLevel"> Zoom level to initialize the quadtree to (zoom level means the same thing as depth in this case) </param>
    public void InitializeQuadTree(int zoomLevel)
    {
        ValidateZoomLevel(zoomLevel);
        m_camera.CurrentZoomLevel = zoomLevel;

        lock (rootNodeLock) // We must lock this as TerrainQuadTreeUpdater also has access to the array of root nodes
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
                n.Name = $"TerrainQuadTreeNode_{i}";
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

            AddChild(node);
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

        if (!node.HasAllChildren())
        {
            GenerateChildNodes(node);
            foreach (var childNode in node.ChildNodes)
            {
                InitializeTerrainNodeMesh(childNode);
            }
        }

        foreach (var childNode in node.ChildNodes)
        {
            childNode.IsVisible = true;
            childNode.Chunk.Visible = true;
            childNode.Chunk.TerrainChunkMesh.SortingOffset = CHUNK_SORT_OFFSET * childNode.Depth;
        }

        node.Chunk.TerrainChunkMesh.SortingOffset = -CHUNK_SORT_OFFSET * node.Depth;
        node.IsVisible = false;
    }

    /// <summary>
    /// Merges a parent nodes' children into itself. This simply toggles the visibility of the parent to true,
    /// and the visibility of the children to false.
    /// </summary>
    /// <param name="parent">The parent to merge its children into</param>
    private void MergeNodeChildren(TerrainQuadTreeNode parent)
    {
        if (!GodotUtils.IsValid(parent)) { return; }

        parent.Chunk.TerrainChunkMesh.SortingOffset = CHUNK_SORT_OFFSET * parent.Depth;
        parent.Chunk.Visible = true;
        parent.IsVisible = true;

        foreach (var childNode in parent.ChildNodes)
        {
            if (GodotUtils.IsValid(childNode))
            {
                childNode.Chunk.Visible = false;
                childNode.IsVisible = false;
                childNode.Chunk.TerrainChunkMesh.SortingOffset = -CHUNK_SORT_OFFSET * childNode.Depth;
            }
        }
    }

    /// <summary>
    /// Initializes the mesh of a TerrainQuadTreeNode. Does nothing if the mesh is already initialized,
    /// otherwise creates a new one for it based on the internal TerrainChunks properties.
    /// </summary>
    /// <param name="node">The node to initialize the mesh of (for its TerrainChunk)</param>
    /// <exception cref="ArgumentNullException"></exception>
    private void InitializeTerrainNodeMesh(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node) || !GodotUtils.IsValid(node.Chunk))
        {
            throw new ArgumentNullException("Trying to initialize terrain node mesh that is null");
        }

        ValidateTerrainNodeForMeshInitialization(node);

        // If the mesh is invalid this means this is the very first time we are loading up this node into the
        // scene tree
        if (!GodotUtils.IsValid(node.Chunk.MeshInstance))
        {
            ArrayMesh meshSegment = GenerateMeshForNode(node);
            node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
            // AddChild(node);

            node.Chunk.SetPositionAndSize(); // Set the position of the chunk itself
            node.SetPosition(node.Chunk.Position); // Set the position of the node (copy chunk position)

            node.Chunk.Name = GenerateChunkName(node);

            node.Chunk.Load();
        }

        node.IsVisible = true;
        node.Chunk.Visible = true;
    }

    private ArrayMesh GenerateMeshForNode(TerrainQuadTreeNode node)
    {
        ArrayMesh meshSegment =
            WGS84EllipsoidMeshGenerator
                .CreateEllipsoidMeshSegment(
                    (float)node.Chunk.MapTile.Latitude,
                    (float)node.Chunk.MapTile.Longitude,
                    (float)node.Chunk.MapTile.LatitudeRange,
                    (float)node.Chunk.MapTile.LongitudeRange
                );
        return meshSegment;
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

    /// <summary>
    /// Generates child nodes for the input TerrainQuadTreeNode. Initializes both the inner TerrainChunk
    /// and MapTile based on the information inside the given TerrainQuadTreeNode
    /// </summary>
    /// <param name="parentNode">Parent node to generate children for</param>
    /// <exception cref="ArgumentNullException"></exception>
    private void GenerateChildNodes(TerrainQuadTreeNode parentNode)
    {
        if (!GodotUtils.IsValid(parentNode))
        {
            throw new ArgumentNullException(nameof(parentNode), "Cannot generate children for a null node.");
        }

        if (!GodotUtils.IsValid(parentNode.Chunk))
        {
            throw new ArgumentNullException(nameof(parentNode),
                "Cannot generate children for a node with a null terrain chunk.");
        }

        if (parentNode.Chunk.MapTile == null)
        {
            throw new ArgumentNullException(nameof(parentNode),
                "Cannot generate children for a node with a null map tile in its terrain chunk.");
        }

        int parentLatTileCoo = parentNode.Chunk.MapTile.LatitudeTileCoo;
        int parentLonTileCoo = parentNode.Chunk.MapTile.LongitudeTileCoo;
        int childZoomLevel = parentNode.Chunk.MapTile.ZoomLevel + 1;

        for (int i = 0; i < 4; i++)
        {
            (int childLatTileCoo, int childLonTileCoo) =
                CalculateChildTileCoordinates(parentLatTileCoo, parentLonTileCoo, i);
            parentNode.ChildNodes[i] = CreateNode(childLatTileCoo, childLonTileCoo, childZoomLevel);
            parentNode.AddChild(parentNode.ChildNodes[i]);
        }
    }

    private (int childLatTileCoo, int childLonTileCoo) CalculateChildTileCoordinates(int parentLatTileCoo,
        int parentLonTileCoo, int childIndex)
    {
        int childLatTileCoo = parentLatTileCoo * 2 + ((childIndex == 2 || childIndex == 3) ? 1 : 0);
        int childLonTileCoo = parentLonTileCoo * 2 + ((childIndex == 1 || childIndex == 3) ? 1 : 0);
        return (childLatTileCoo, childLonTileCoo);
    }

    /// <summary>
    /// Creates a TerrainQuadTree node and initializes the TerrainChunk with a corresponding MapTile
    /// </summary>
    /// <param name="latTileCoo">The latitude tile coordinate of the MapTile</param>
    /// <param name="lonTileCoo">The longitude tile coordinate of the MapTile</param>
    /// <param name="zoomLevel">The zoom level of the MapTile/TerrainChunk/TerrainQuadTreeNode</param>
    /// <returns></returns>
    private TerrainQuadTreeNode CreateNode(int latTileCoo, int lonTileCoo, int zoomLevel)
    {
        double childCenterLat = MapUtils.ComputeCenterLatitude(latTileCoo, zoomLevel);
        double childCenterLon = MapUtils.ComputeCenterLongitude(lonTileCoo, zoomLevel);

        var childChunk = new TerrainChunk(new MapTile((float)childCenterLat, (float)childCenterLon, zoomLevel));
        return new TerrainQuadTreeNode(childChunk, zoomLevel);
    }

    /// <summary>
    /// Called when the TerrainQuadTreeUpdater has finished a tree traversal iteration and has determined which
    /// nodes should be split/merged. After this, we are allowed to actually merge/split the nodes in question
    /// on the main thread in the scene tree.
    /// </summary>
    private void OnQuadTreeUpdatesDetermined()
    {
        m_canUpdateQuadTree = true;
    }
}
