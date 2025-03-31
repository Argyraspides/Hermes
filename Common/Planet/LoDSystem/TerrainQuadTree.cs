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

using System.Threading;

namespace Hermes.Common.Planet.LoDSystem;

using Godot;
using System;

using Hermes.Common.Map.Types;
using Hermes.Common.Map.Utils;
using Hermes.Common.GodotUtils;
using Hermes.Common.Meshes.MeshGenerators;
using Hermes.Universe.SolarSystem;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
/// </summary>
public sealed partial class TerrainQuadTree : Node3D
{
    // Current minimum and maximum depths allowed
    public int MaxDepth { get; private set; }
    public int MinDepth { get; private set; }

    // Maximum allowed nodes in the scene tree
    public long MaxNodes { get; private set; }

    // Thresholds below/above which we will split/merge nodes respectively. Each index represents a zoom level,
    // the value at the index represents the threshold in kilometers
    public double[] SplitThresholds { get; private set; }
    public double[] MergeThresholds { get; private set; }

    // Current amount of nodes in the scene tree (in total -- not just the quadtree)
    public int CurrentNodeCount { get; private set; }
    // Mutex to access the root nodes
    public object RootNodeLock = new object();

    private ManualResetEventSlim m_canUpdateQuadTree = new ManualResetEventSlim(false);

    // List of root nodes. We are allowed a minimum zoom level of above 1, thus all nodes at zoom level 'z',
    // where 1 < z < m_maxDepth are unnecessary to keep in memory. This is a list of all the root nodes at the minimum
    // depth where we can start a quadtree traversal
    public List<TerrainQuadTreeNode> RootNodes { get; private set; }

    // Reading inherent properties of nodes is not thread-safe in Godot. Here we make a custom Vector3
    // which is a copy of the camera's position updated from the TerrainQuadTree thread, so that it can
    // be accessed by the TerrainQuadTreeUpdater thread safely
    public Vector3 CameraPosition { get; private set; }

    // Queue of nodes that should be split/merged as determined by the TerrainQuadTreeUpdater
    public ConcurrentQueue<TerrainQuadTreeNode> SplitQueueNodes = new ConcurrentQueue<TerrainQuadTreeNode>();
    public ConcurrentQueue<TerrainQuadTreeNode> MergeQueueNodes = new ConcurrentQueue<TerrainQuadTreeNode>();

    // If we hit x% of the maximum allowed amount of nodes, we will begin culling unused nodes in the quadtree
    public float MaxNodesCleanupThresholdPercent { get; private set; } = 0.90F;

    // Maximum amount of split and merge operations allowed per frame in the scene tree.
    // This is to spread the workload of splitting/merging over multiple frames
    private const int MaxQueueUpdatesPerFrame = 6;

    // To prevent hysterisis and oscillation between merging/splitting at fine boundaries, we multiply the merge
    // thresholds to be greater than the split thresholds
    private const float MergeThresholdFactor = 1.15F;

    // Hard limit of allowed depth of the quadtree
    private const int MAX_DEPTH_LIMIT = 23;

    // Hard limit of minimum allowed depth of the quadtree
    private const int MIN_DEPTH_LIMIT = 1;

    // To prevent seams, we keep only the parent chunk of the current deepest visible.
    // This is the offset used to determine the draw order (children should be drawn after parents)
    private const float CHUNK_SORT_OFFSET = 10.0f;

    // TODO(Argyrsapides, 22/02/2025): Make this a configurable curve or something
    private readonly double[] m_baseAltitudeThresholds = new double[]
    {
        156000.0f, 78000.0f, 39000.0f, 19500.0f, 9750.0f, 4875.0f, 2437.5f, 1218.75f, 609.375f, 304.6875f, 152.34f,
        76.17f, 38.08f, 19.04f, 9.52f, 4.76f, 2.38f, 1.2f, 0.6f, 0.35f
    };

    private readonly PlanetOrbitalCamera m_camera;
    private readonly TerrainQuadTreeUpdater m_quadTreeUpdater;

    // True if the TerrainQuadTree is about to be destroyed. Used as we don't want to update our current node count
    // when the game is closing and nodes in the scene tree may be invalid
    private bool m_destructorActivated = false;

    public TerrainQuadTree(PlanetOrbitalCamera camera, int maxNodes = 7500, int minDepth = 6, int maxDepth = 20)
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

        m_camera = camera ?? throw new ArgumentNullException(nameof(camera));
        MaxNodes = maxNodes;
        MinDepth = minDepth;
        MaxDepth = maxDepth;

        InitializeAltitudeThresholds();
        m_quadTreeUpdater = new TerrainQuadTreeUpdater(this, m_canUpdateQuadTree);
    }

    public override void _Process(double delta)
    {
        CameraPosition = m_camera.Position;

        for (int i = m_baseAltitudeThresholds.Length - 1; i > 1; i--)
        {
            if (m_baseAltitudeThresholds[i] < m_camera.CurrentAltitude  &&
                m_baseAltitudeThresholds[i - 1] > m_camera.CurrentAltitude )
            {
                m_camera.CurrentZoomLevel = i;
                break;
            }
        }

        if (m_canUpdateQuadTree.IsSet)
        {
            lock (RootNodeLock)
            {
                ProcessSplitQueue();
                ProcessMergeQueue();
            }

            if (SplitQueueNodes.IsEmpty && MergeQueueNodes.IsEmpty)
            {
                m_canUpdateQuadTree.Reset();
                m_quadTreeUpdater.CanPerformCulling.Set();
            }
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
            CurrentNodeCount = GetTree().GetNodeCount();
        }
    }

    /// <summary>
    /// Initializes the quadtree at the specified zoom level. Note that if the current minimum zoom level
    /// is greater than one, then all nodes below this zoom level will never exist in the scene tree
    /// </summary>
    /// <param name="zoomLevel"> Zoom level to initialize the quadtree to (zoom level means the same thing as depth in this case) </param>
    public void InitializeQuadTree(int zoomLevel)
    {
        if (zoomLevel > MaxDepth || zoomLevel < MinDepth)
        {
            throw new ArgumentException($"zoomLevel must be between {MinDepth} and {MaxDepth}");
        }
        m_camera.CurrentZoomLevel = zoomLevel;

        lock (RootNodeLock)
        {
            Queue<TerrainQuadTreeNode> nodeQueue = new Queue<TerrainQuadTreeNode>();
            RootNodes = new List<TerrainQuadTreeNode>();

            int nodesPerSide = (1 << MinDepth); // 2^z
            int nodesInLevel = nodesPerSide * nodesPerSide; // 4^z
            for (int i = 0; i < nodesInLevel; i++)
            {
                int latTileCoo = i / nodesPerSide;
                int lonTileCoo = i % nodesPerSide;
                TerrainQuadTreeNode n = CreateNode(latTileCoo, lonTileCoo, MinDepth);
                n.Name = $"TerrainQuadTreeNode_{latTileCoo}_{lonTileCoo}";
                RootNodes.Add(n);
                nodeQueue.Enqueue(RootNodes[i]);
            }

            for (int zLevel = MinDepth; zLevel < zoomLevel; zLevel++)
            {
                nodesInLevel = 1 << (2 * zLevel); // 4^z
                for (int n = 0; n < nodesInLevel; n++)
                {
                    TerrainQuadTreeNode parentNode = nodeQueue.Dequeue();
                    GenerateChildNodes(parentNode);
                    foreach (var childNode in parentNode.ChildNodes)
                    {
                        nodeQueue.Enqueue(childNode);
                    }
                }
            }

            while (nodeQueue.Count > 0)
            {
                TerrainQuadTreeNode node = nodeQueue.Dequeue();
                AddChild(node);
                InitializeTerrainNodeMesh(node);
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
        bool invalidNode =
            !GodotUtils.IsValid(node) ||
            !GodotUtils.IsValid(node.Chunk) ||
            node.Chunk.MapTile == null;
        if (invalidNode)
        {
            throw new ArgumentNullException("Cannot initialize terrain mesh because node is invalid");
        }

        // If the mesh is invalid this means this is the very first time we are loading up this node into the
        // scene tree
        if (!GodotUtils.IsValid(node.Chunk.MeshInstance))
        {
            ArrayMesh meshSegment = GenerateMeshForNode(node);
            node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };

            node.Chunk.SetPositionAndSize();        // Set the position of the chunk itself
            node.Position = node.Chunk.Position;    // Set the position of the node (copy chunk position)

            node.Chunk.Name = GenerateChunkName(node);

            node.Chunk.Load();
        }

        node.IsDeepest = true;
        node.Chunk.Visible = true;
    }

    private void InitializeAltitudeThresholds()
    {
        SplitThresholds = new double[MaxDepth + 1];
        MergeThresholds = new double[MaxDepth + 2];

        for (int zoom = 0; zoom < MaxDepth; zoom++)
        {
            SplitThresholds[zoom] = m_baseAltitudeThresholds[zoom];
        }

        for (int zoom = 1; zoom < MaxDepth; zoom++)
        {
            MergeThresholds[zoom] = SplitThresholds[zoom - 1] * MergeThresholdFactor;
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

    /// <summary>
    /// Splits the quad tree nodes by initializing its children. If its children already exists, simply
    /// toggles their visibility on and itself off.
    /// </summary>
    /// <param name="node">Node to be split</param>
    /// <exception cref="ArgumentNullException">Thrown if the TerrainQuadTreeNode is not valid</exception>
    private void SplitNode(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node))
        {
            return;
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
            childNode.IsDeepest = true;
            childNode.Chunk.Visible = true;
            childNode.Chunk.TerrainChunkMesh.SortingOffset = CHUNK_SORT_OFFSET * childNode.Depth;
        }

        node.Chunk.TerrainChunkMesh.SortingOffset = -CHUNK_SORT_OFFSET * node.Depth;
        // Don't toggle parent visibility off to prevent gaps between meshes of different
        // zoom levels
        // TODO::ARGYRASPIDES() { Find a way to make sure that only the parent of the deepest node remains
        // visible, and not every parent node up until the deepest node }
        node.IsDeepest = false;
    }

    private void MergeNodeChildren(TerrainQuadTreeNode parent)
    {
        if (!GodotUtils.IsValid(parent))
        {
            return;
        }

        parent.Chunk.TerrainChunkMesh.SortingOffset = CHUNK_SORT_OFFSET * parent.Depth;
        parent.Chunk.Visible = true;
        parent.IsDeepest = true;

        foreach (var childNode in parent.ChildNodes)
        {
            if (GodotUtils.IsValid(childNode))
            {
                childNode.Chunk.Visible = false;
                childNode.IsDeepest = false;
                childNode.Chunk.TerrainChunkMesh.SortingOffset = -CHUNK_SORT_OFFSET * childNode.Depth;
            }
        }
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

    private TerrainQuadTreeNode CreateNode(int latTileCoo, int lonTileCoo, int zoomLevel)
    {
        double childCenterLat = MapUtils.ComputeCenterLatitude(latTileCoo, zoomLevel);
        double childCenterLon = MapUtils.ComputeCenterLongitude(lonTileCoo, zoomLevel);

        var childChunk = new TerrainChunk(new MapTile((float)childCenterLat, (float)childCenterLon, zoomLevel));
        return new TerrainQuadTreeNode(childChunk, zoomLevel);
    }
}
