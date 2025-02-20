using System;
using System.Threading;
using Godot;

namespace Hermes.Common.Planet.LoDSystem;

public partial class TerrainQuadTreeUpdater : Node
{
    #region Dependencies & State

    private readonly TerrainQuadTree m_terrainQuadTree;
    private readonly int m_quadTreeUpdateIntervalMs = 250;
    private volatile bool m_isRunning = false;
    private volatile bool m_canPerformSearch = true;

    public Thread UpdateQuadTreeThread { get; private set; }
    public Thread CullQuadTreeThread { get; private set; }

    #endregion Dependencies & State

    #region Signals

    [Signal]
    public delegate void QuadTreeUpdatesDeterminedEventHandler();

    #endregion Signals

    #region Constructor & Thread Management

    public TerrainQuadTreeUpdater(TerrainQuadTree terrainQuadTree)
    {
        m_terrainQuadTree = terrainQuadTree ?? throw new ArgumentNullException(nameof(terrainQuadTree));
        m_terrainQuadTree.QuadTreeUpdated += OnQuadTreeUpdated;
        StartUpdateThread();
    }

    private void StartUpdateThread()
    {
        UpdateQuadTreeThread = new Thread(UpdateQuadTreeThreadFunction)
        {
            IsBackground = true, Name = "QuadTreeUpdateThread"
        };

        CullQuadTreeThread =
            new Thread(StartCullingThreadFunction) { IsBackground = true, Name = "CullQuadTreeThread" };

        UpdateQuadTreeThread.Start();
        CullQuadTreeThread.Start();
        m_isRunning = true;
    }

    private void StartCullingThreadFunction()
    {
        while (m_isRunning)
        {
            try
            {
                if (m_canPerformSearch && m_terrainQuadTree.RootNodes != null)
                {
                    lock (m_terrainQuadTree.rootNodeLock)
                    {
                        foreach (var rootNode in m_terrainQuadTree.RootNodes)
                        {
                            if (GodotUtils.IsValid(rootNode) && ExceedsMaxNodeThreshold())
                            {
                                CullUnusedNodes(rootNode);
                            }
                        }
                    }
                }

                Thread.Sleep(m_quadTreeUpdateIntervalMs);
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

    #endregion Constructor & Thread Management

    #region Update Logic

    private void UpdateQuadTreeThreadFunction()
    {
        while (m_isRunning)
        {
            try
            {
                if (m_canPerformSearch && m_terrainQuadTree.RootNodes != null)
                {
                    lock (m_terrainQuadTree.rootNodeLock)
                    {
                        foreach (var rootNode in m_terrainQuadTree.RootNodes)
                        {
                            if (GodotUtils.IsValid(rootNode))
                            {
                                UpdateTreeDFS(rootNode);
                            }
                        }
                    }
                }

                if (m_canPerformSearch)
                {
                    EmitSignal(SignalName.QuadTreeUpdatesDetermined);
                    m_canPerformSearch = false;
                }

                Thread.Sleep(m_quadTreeUpdateIntervalMs);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in quadtree update thread: {ex}");
            }
        }
    }

    private void UpdateTreeDFS(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) { return; }

        // Splitting happens top-down, so we do it first prior to recursing down further
        if (node.IsVisible && ShouldSplit(node))
        {
            m_terrainQuadTree.SplitQueueNodes.Enqueue(node);
            ArrayMesh m = GenerateMeshForNode(node);
            return;
        }

        if (!node.IsVisible)
        {
            foreach (var childNode in node.ChildNodes)
            {
                UpdateTreeDFS(childNode);
            }
        }

        // Merging happens bottom-up, so we do it after recursing down the tree
        if (ShouldMergeChildren(node))
        {
            m_terrainQuadTree.MergeQueueNodes.Enqueue(node);
        }
    }

    private bool ExceedsMaxNodeThreshold() => m_terrainQuadTree.m_currentNodeCount >
                                              m_terrainQuadTree.m_maxNodes *
                                              m_terrainQuadTree.MaxNodesCleanupThresholdPercent;

    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) throw new ArgumentNullException(nameof(node), "node cannot be null");
        if (node.Depth >= m_terrainQuadTree.m_maxDepth) return false;

        float distanceToCamera = node.Position.DistanceTo(m_terrainQuadTree.CameraPosition);
        return m_terrainQuadTree.m_splitThresholds[node.Depth] > distanceToCamera;
    }

    private bool ShouldMerge(TerrainQuadTreeNode node)
    {
        if (!GodotUtils.IsValid(node)) return false;
        if (node.Depth <= m_terrainQuadTree.m_minDepth + 1) return false;

        float distanceToCamera = node.Position.DistanceTo(m_terrainQuadTree.CameraPosition);
        return m_terrainQuadTree.m_mergeThresholds[node.Depth - 1] < distanceToCamera;
    }

    private bool ShouldMergeChildren(TerrainQuadTreeNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node), "node cannot be null");

        bool shouldAllChildrenMerge = false;
        foreach (var childNode in node.ChildNodes)
        {
            shouldAllChildrenMerge |= ShouldMerge(childNode);
        }

        return shouldAllChildrenMerge;
    }

    #endregion Update Logic

    #region Node Management

    private void RemoveQuadTreeNode(TerrainQuadTreeNode node)
    {
        if (GodotUtils.IsValid(node))
        {
            node.CallDeferred("queue_free");
        }
    }

    private void RemoveSubQuadTreeThreadSafe(TerrainQuadTreeNode parent)
    {
        if (parent == null) return;

        foreach (var childNode in parent.ChildNodes)
        {
            RemoveSubQuadTreeThreadSafe(childNode);
            RemoveQuadTreeNode(childNode);
        }
    }

    private void CullUnusedNodes(TerrainQuadTreeNode parentNode)
    {
        if (parentNode == null) return;

        if (parentNode.IsVisible)
        {
            RemoveSubQuadTreeThreadSafe(parentNode);
            return;
        }

        foreach (var terrainQuadTreeNode in parentNode.ChildNodes)
        {
            if (terrainQuadTreeNode != null)
            {
                CullUnusedNodes(terrainQuadTreeNode);
            }
        }
    }

    private ArrayMesh GenerateMeshForNode(TerrainQuadTreeNode node)
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
    }

    #endregion Node Management

    private void OnQuadTreeUpdated()
    {
        m_canPerformSearch = true;
    }
}
