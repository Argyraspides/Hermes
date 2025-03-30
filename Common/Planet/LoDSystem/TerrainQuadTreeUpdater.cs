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

using Hermes.Common.Map.Utils;
using Hermes.Common.Meshes.MeshGenerators;

namespace Hermes.Common.Planet.LoDSystem;

using System;
using System.Threading;
using Godot;
using Hermes.Common.GodotUtils;

/// <summary>
/// The TerrainQuadTreeUpdater is meant to perform the following tasks:
///
/// - Determining which nodes in the TerrainQuadTree need to be split/merged (job 1)
/// - Determining which nodes in the TerrainQuadTree need to be culled if we exceed the maximum node count (job 2)
///
/// Both these tasks run on separate threads, also separate from the TerrainQuadTree which runs on the main thread
///
/// The flow is as follows:
///
/// TerrainQuadTree goes through its queue to split/merge nodes ->
/// TerrainQuadTree signals to TerrainQuadTreeUpdater that it has finished splitting/merging nodes ->
/// m_canPerformCulling becomes true ->
/// We traverse the tree to cull all unused nodes ->
/// TerrainQuadTreeUpdater signals to itself that it has finished culling all nodes ->
/// m_canPerformSearch becomes true ->
/// We traverse the tree to see which nodes should be split/merged, and add them to the TerrainQuadTree's queues ->
/// We signal to the TerrainQuadTree that we have finished determining which ndoes should be split/merged ->
/// TerrainQuadTree goes through its queue to split/merge nodes ->
///
/// Repeat ...
///
/// TerrainQuadTreeUpdater and TerrainQuadTree are tightly coupled by design. Traversing the quadtree is an expensive
/// procedure and shouldn't be done on the main thread
///
/// </summary>
public partial class TerrainQuadTreeUpdater : Node
{

    // Signal emitted when we are done determining which nodes should be split/merged
    [Signal]
    public delegate void QuadTreeUpdatesDeterminedEventHandler();

    // Signal emitted when we are done culling all unused nodes
    [Signal]
    public delegate void CullQuadTreeFinishedEventHandler();

    public Thread UpdateQuadTreeThread { get; private set; }
    public Thread CullQuadTreeThread { get; private set; }

    private readonly TerrainQuadTree m_terrainQuadTree;

    private bool m_isRunning = false;

    // True if we can perform the DFS search to determine which nodes should be culled, and cull them
    private ManualResetEvent m_canPerformCulling = new ManualResetEvent(false);

    // True if we can perform the DFS search to determine which nodes should be split/merged
    private ManualResetEvent m_canPerformSearch = new ManualResetEvent(true);


    public TerrainQuadTreeUpdater(TerrainQuadTree terrainQuadTree)
    {
        m_terrainQuadTree = terrainQuadTree ?? throw new ArgumentNullException(nameof(terrainQuadTree));

        m_terrainQuadTree.QuadTreeUpdated += OnQuadTreeUpdated;
        CullQuadTreeFinished += OnCullingFinished;

        StartUpdateThread();
    }

    private void StartUpdateThread()
    {
        UpdateQuadTreeThread = new Thread(UpdateQuadTreeThreadFunction)
        {
            IsBackground = true, Name = "QuadTreeUpdateThread"
        };

        CullQuadTreeThread = new Thread(StartCullingThreadFunction)
        {
            IsBackground = true, Name = "CullQuadTreeThread"
        };

        UpdateQuadTreeThread.Start();
        CullQuadTreeThread.Start();
        m_isRunning = true;
    }

    private void StartCullingThreadFunction()
    {
        while (m_isRunning)
        {
            m_canPerformCulling.WaitOne();
            try
            {
                if (m_terrainQuadTree.RootNodes == null) continue;

                lock (m_terrainQuadTree.RootNodeLock)
                {
                    foreach (var rootNode in m_terrainQuadTree.RootNodes)
                    {
                        if (!GodotUtils.IsValid(rootNode) || !ExceedsMaxNodeThreshold()) continue;
                        CullUnusedNodes(rootNode);
                    }
                }

                EmitSignal(SignalName.CullQuadTreeFinished);
                m_canPerformCulling.Reset();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in quadtree update thread: {ex}");
            }
        }
    }

    public void StopUpdateThread()
    {
        m_isRunning = false;
        if (UpdateQuadTreeThread != null && UpdateQuadTreeThread.IsAlive)
        {
            UpdateQuadTreeThread.Join(1000);
        }

        if (CullQuadTreeThread != null && CullQuadTreeThread.IsAlive)
        {
            CullQuadTreeThread.Join(1000);
        }
    }

    private void UpdateQuadTreeThreadFunction()
    {
        while (m_isRunning)
        {
            m_canPerformSearch.WaitOne();
            try
            {
                if(m_terrainQuadTree.RootNodes == null) continue;

                lock (m_terrainQuadTree.RootNodeLock)
                {
                    foreach (var rootNode in m_terrainQuadTree.RootNodes)
                    {
                        if (!GodotUtils.IsValid(rootNode)) continue;
                        UpdateTreeDFS(rootNode);
                    }
                }

                EmitSignal(SignalName.QuadTreeUpdatesDetermined);
                m_canPerformSearch.Reset();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in quadtree update thread: {ex}");
            }
        }
    }

    private void UpdateTreeDFS(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) return;

        // Splitting happens top-down, so we do it first prior to recursing down further
        if (node.IsDeepest && ShouldSplit(node))
        {
            m_terrainQuadTree.SplitQueueNodes.Enqueue(node);
            return;
        }

        foreach (var childNode in node.ChildNodes)
        {
            UpdateTreeDFS(childNode);
        }

        // Merging happens bottom-up, so we do it after recursing down the tree
        if (ShouldMergeChildren(node))
        {
            m_terrainQuadTree.MergeQueueNodes.Enqueue(node);
        }
    }

    private bool ExceedsMaxNodeThreshold()
    {
        return m_terrainQuadTree.CurrentNodeCount >
               m_terrainQuadTree.MaxNodes *
               m_terrainQuadTree.MaxNodesCleanupThresholdPercent;
    }

    // TODO::ARGYRASPIDES() { Make these not just distance based but also based on what is visible on the screen.
    // Sometimes the center of the screen is more detailed than the rest and it can look jarring if the map tiles
    // are from completely different times }
    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) throw new ArgumentNullException(nameof(node), "node cannot be null");
        if (node.Depth >= m_terrainQuadTree.MaxDepth) return false;

        float distanceToCamera = node.Position.DistanceTo(m_terrainQuadTree.CameraPosition);
        bool shouldSplit = m_terrainQuadTree.SplitThresholds[node.Depth] > distanceToCamera;

        return shouldSplit;
    }

    private bool ShouldMerge(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) return false;
        if (node.Depth < m_terrainQuadTree.MinDepth) return false;

        float distanceToCamera = node.Position.DistanceTo(m_terrainQuadTree.CameraPosition);
        bool shouldMerge = m_terrainQuadTree.MergeThresholds[node.Depth] < distanceToCamera;

        return shouldMerge;
    }

    /// <summary>
    /// Checks if we should merge the children of the terrain quad tree node.
    /// In the LoD system, we only split as far as we need to, thus leaf nodes are
    /// the ones visible in the scene tree. We only merge the children back into the parent
    /// if ALL children are too far from the camera.
    /// </summary>
    /// <param name="parentNode">Parent node whose children will be tested for merging</param>
    /// <returns>True if the parents children should be merged, otherwise false</returns>
    private bool ShouldMergeChildren(TerrainQuadTreeNode parentNode)
    {
        if (!GodotUtils.IsValid(parentNode)) return false;

        foreach (var childNode in parentNode.ChildNodes)
        {
            if (!GodotUtils.IsValid(childNode) || !ShouldMerge(childNode))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Removes a quadtree node from the scene tree itself. The object will be deleted in the Godot world,
    /// but not in the C# world
    /// </summary>
    /// <param name="node"></param>
    private void RemoveQuadTreeNode(TerrainQuadTreeNode node)
    {
        if (GodotUtils.IsValid(node))
        {
            node.CallDeferred("queue_free");
        }
    }

    /// <summary>
    /// Completely removes the entire subtree of the parent node
    /// </summary>
    /// <param name="parent"></param>
    private void RemoveSubQuadTreeThreadSafe(TerrainQuadTreeNode parent)
    {
        if (!GodotUtils.IsValid(parent)) return;

        foreach (var childNode in parent.ChildNodes)
        {
            RemoveSubQuadTreeThreadSafe(childNode);
            RemoveQuadTreeNode(childNode);
        }
    }

    /// <summary>
    /// Culls any unused nodes in the scene tree. An unused node is any node which is not visible (hence not
    /// useful to the player) AND has no visible ancestors.
    /// </summary>
    /// <param name="parentNode">The parent node whose entire subtree will be culled</param>
    private void CullUnusedNodes(TerrainQuadTreeNode parentNode)
    {
        if (!GodotUtils.IsValid(parentNode)) return;

        // We only want to cull nodes BELOW the ones that are currently visible in the scene
        if (parentNode.IsDeepest)
        {
            // Cull all sub-trees below the parent
            RemoveSubQuadTreeThreadSafe(parentNode);
            return;
        }

        // Recursively destroy all nodes
        foreach (var terrainQuadTreeNode in parentNode.ChildNodes)
        {
            if (GodotUtils.IsValid(terrainQuadTreeNode))
            {
                CullUnusedNodes(terrainQuadTreeNode);
            }
        }
    }

    /// <summary>
    /// Generates a mesh for the TerrainChunk of the corresponding quadtree node. This is used for spherical
    /// planets whose surface is represented with meshes.
    /// </summary>
    /// <param name="node"> Node for which we want to generate a mesh for its terrain chunk </param>
    /// <returns>Returns an ArrayMesh representing the mesh of the TerrainChunk</returns>
    private ArrayMesh GenerateMeshForNode(TerrainQuadTreeNode node)
    {
        // TODO::ARGYRASPIDES() { This is specifically for the earth. The terrain quad tree should know
        // about what kind of planet it is dealing with, and MapUtils needs to be changed to determine what to do based
        // on planet type }
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

    /// <summary>
    /// Called via signal when the TerrainQuadTree finishes splitting/merging all nodes in its queue
    /// </summary>
    private void OnQuadTreeUpdated()
    {
        m_canPerformCulling.Set();
    }

    /// <summary>
    ///  Called via signal when the TerrainQuadTreeUpdater finishes culling all nodes in the scene tree (one iteration)
    /// </summary>
    private void OnCullingFinished()
    {
        m_canPerformSearch.Set();
    }
}
