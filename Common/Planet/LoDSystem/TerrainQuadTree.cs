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
using System.Threading.Tasks;
using Godot;

namespace Hermes.Common.Planet.LoDSystem;

public partial class TerrainQuadTree : Node
{
    #region Constants & Configuration

    public float MaxNodesCleanupThresholdPercent = 0.90F;
    public const int MaxQueueUpdatesPerFrame = 25;
    public const float MergeThresholdFactor = 2.05F;
    public const int MaxDepthLimit = 23;
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
    public readonly int m_maxDepth;
    public readonly int m_minDepth;

    public long m_maxNodes;
    public double[] m_splitThresholds;
    public double[] m_mergeThresholds;

    public volatile int m_currentNodeCount = 0;
    public volatile bool m_isRunning = false;
    public volatile bool m_destructorActivated = false;
    public bool m_canUpdateQuadTree = false;

    public TerrainQuadTreeNode RootNode { get; private set; }

    public Vector3 CameraPosition { get; private set; }
    public ConcurrentQueue<TerrainQuadTreeNode> SplitQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();
    public ConcurrentQueue<ArrayMesh> SplitQueueMeshes { get; } = new ConcurrentQueue<ArrayMesh>();
    public ConcurrentQueue<TerrainQuadTreeNode> MergeQueueNodes { get; } = new ConcurrentQueue<TerrainQuadTreeNode>();

    #endregion Dependencies & State

    #region Signals

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
        m_isRunning = true;

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
        m_isRunning = false;
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

        RootNode = CreateRootNode();
        Queue<TerrainQuadTreeNode> nodeQueue = new Queue<TerrainQuadTreeNode>();
        nodeQueue.Enqueue(RootNode);

        for (int zLevel = 0; zLevel < zoomLevel; zLevel++)
        {
            int nodesInLevel = 1 << (2 * zLevel); // 4^z
            for (int n = 0; n < nodesInLevel; n++)
            {
                TerrainQuadTreeNode parentNode = nodeQueue.Dequeue();
                GenerateChildNodes(parentNode);
                EnqueueChildren(nodeQueue, parentNode);
            }
        }

        InitializeMeshesInQueue(nodeQueue);
    }

    private TerrainQuadTreeNode CreateRootNode()
    {
        var rootChunk = new TerrainChunk(new MapTile(0.0F, 0.0F, 0));
        return new TerrainQuadTreeNode(rootChunk, 0);
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

    private void SplitNode(TerrainQuadTreeNode node)
    {
        GenerateChildNodes(node);
        foreach (var childNode in node.ChildNodes)
        {
            if (childNode != null)
            {
                InitializeTerrainNodeMesh(childNode);
            }
        }

        node.Chunk.Visible = false;
        node.IsLoadedInScene = false;
    }

    private void MergeNodeChildren(TerrainQuadTreeNode parent)
    {
        if (!IsInstanceValid(parent)) return;

        parent.Chunk.Visible = true;
        parent.IsLoadedInScene = true;

        foreach (var childNode in parent.ChildNodes)
        {
            if (IsInstanceValid(childNode))
            {
                childNode.Chunk.Visible = false;
                childNode.IsLoadedInScene = false;
            }
        }
    }

    private async void InitializeTerrainNodeMesh(TerrainQuadTreeNode node)
    {
        ValidateTerrainNodeForMeshInitialization(node);

        ArrayMesh meshSegment = await GenerateMeshForNode(node);

        node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        node.Chunk.Load();
        AddChild(node);
        node.Chunk.SetPositionAndSize();
        node.SetPosition(node.Chunk.Position);
        node.IsLoadedInScene = true;
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
        if (node == null) throw new ArgumentNullException(nameof(node), "Cannot initialize mesh for a null node.");
        if (node.Chunk == null) throw new ArgumentNullException(nameof(node.Chunk), "Node's chunk is null.");
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
            parentNode.ChildNodes[i] = CreateChildNode(childLatTileCoo, childLonTileCoo, childZoomLevel);
        }
    }

    private (int childLatTileCoo, int childLonTileCoo) CalculateChildTileCoordinates(int parentLatTileCoo,
        int parentLonTileCoo, int childIndex)
    {
        int childLatTileCoo = parentLatTileCoo * 2 + ((childIndex == 2 || childIndex == 3) ? 1 : 0);
        int childLonTileCoo = parentLonTileCoo * 2 + ((childIndex == 1 || childIndex == 3) ? 1 : 0);
        return (childLatTileCoo, childLonTileCoo);
    }

    private TerrainQuadTreeNode CreateChildNode(int childLatTileCoo, int childLonTileCoo, int childZoomLevel)
    {
        // TODO(Argyraspides, 19/02/2025): Abstract away Mercator/WGS84 specifics from TerrainQuadTree
        double childLat = MapUtils.MapTileToLatitude(childLatTileCoo, childZoomLevel);
        double childLon = MapUtils.MapTileToLongitude(childLonTileCoo, childZoomLevel);
        double childLatRange = MapUtils.TileToLatRange(childLatTileCoo, childZoomLevel);
        double childLonRange = MapUtils.TileToLonRange(childZoomLevel);
        double halfChildLatRange = childLatRange / 2;
        double halfChildLonRange = childLonRange / 2;
        double childCenterLat = childLat - halfChildLatRange;
        double childCenterLon = childLon + halfChildLonRange;

        var childChunk = new TerrainChunk(new MapTile((float)childCenterLat, (float)childCenterLon, childZoomLevel));
        return new TerrainQuadTreeNode(childChunk, childZoomLevel);
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
freed them (by using the IsInstanceValid() function), but I haven't actually observed the heap memory usage going down.
Then again, I didn't really watch the profiler for more than a couple minutes.

Bug #4: Happened only a couple times but sometimes I see a big black square as one of the map tiles. I think I caught one when
the Godot debugger said something like "Object reference not set to instance of object" when referring to either a TerrainChunk
or map tile, but I'm not sure

Bug #6: It seems we never cull nodes unless we start zooming out. This causes us to actually exceed our maximum node threshold
sometimes. We should be culling regardless of what happens so long as we don't cull what the user is currently viewing.

Bug #7: In the InitializeTerrainQuadTreeNodeMesh() function, sometimes the node chunk is null when we try and set the position and size,
even though we literally did a null check before entering the function. Idk why.

Bug #8: Tried zooming in way too far one time and it crashed. I don't know why. Error message involved trying to access an
object that was already disposed of
*/
