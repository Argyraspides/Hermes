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
using System.Threading.Tasks;
using Godot;


/**

 Perform DFS
 - If you encounter a node that IS loaded in, that means it is visible in the current game. This means it is the
 lowest resolution node that was allowed given our camera distance at that particular moment in time.
 It also means that all of its children must NOT be visible in the current game.
 - If you encounter a node that is NOT loaded in, that means given the camera distance and FOV and whatever else we are considering,
 it was at a level in the tree which didn't satisfy the level of detail requirements we wanted. This means that somewhere down the line,
 one of its subtrees MUST be loaded in.

 So, if you encounter a node that is NOT loaded in (meaning that it must have a subtree that IS loaded in) then you can ask:
 - Okay, given the current camera distance and FOV and stuff -- does any of this nodes CHILDREN need to be split? We only
 ask this question if any of the children ARE loaded in
    - If it does need to be split, then split it.
    - If it doesn't, don't do anything
 - Okay, given the current camera distance and FOV and stuff -- does any of this nodes  CHILDREN need to be merged back
 into the parent? We only ask this question if any of the children ARE loaded in
    - If it does need to be merged -- then merge it
    - If it doesn't, don't do anything

If it turns out we do NOT need to merge, and we do NOT need to split -- then we can terminate our DFS right here, as
this node satisfies our level of detail requirements.

The reason we ask if any of the CHILDREN need to be split/merged and not just the node we encounter is because in the case
that we want to merge, then we don't need all the children to have a reference to the parent. We can simply "absorb"
the children from the parent node by toggling their visibility off, and toggling the parent visibility on.

And finally -- for memory, if we ever encounter a time where we can terminate our DFS search (so we've hit a node that is
loaded in and doesn't need to have its children split/merged, OR we have JUST split/merged some nodes, meaning that we have
either generated children that are now visible in the scene tree, or we have just merged children back into the parent,
meaning that the parent is now the visible one and it'll have no subtrees that are in the scene tree), AND we have exceeded
the maximum node count, then we can completely cut off all of the current node's children. Since we terminated our DFS here,
then we never needed any of the subtrees anyway. The garbage collector can then work its magic.

 */
public partial class TerrainQuadTree : Node
{
    private PlanetOrbitalCamera m_camera;
    private TerrainQuadTreeNode m_rootNode;

    // Maximum amount of nodes allowed in our quadtree until
    // unused nodes are culled
    private long m_maxNodes;

    private int m_minDepth;

    private int m_maxDepth;

    // Altitude thresholds for when we should start merging/splitting tiles based on altitude (meters)
    private double[] m_altitudeThresholds;

    Thread m_updateQuadTreeThread;

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

        m_camera = camera;
        m_maxDepth = maxDepth;

        // Change this to the desired latitude (in degrees)
        double latitude = 0.0;
        // Convert latitude to radians
        double latitudeRadians = latitude * Math.PI / 180;

        // Compute the altitude thresholds for zoom levels 0 to maxDepth
        m_altitudeThresholds = new double[maxDepth + 1];
        double baseRadius = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Math.Cos(latitudeRadians);

        // Use a non-linear scaling factor to allow for closer zooming
        double scalingFactor = 4.0; // Reduced from 7 to allow closer zooming

        for (int zoom = 0; zoom <= maxDepth; zoom++)
        {
            // Modified threshold calculation with exponential falloff for higher zoom levels
            double zoomScale =
                zoom >= 15 ? Math.Pow(0.85, zoom - 15) : 1.0; // Gradually reduce threshold scaling at high zoom levels

            double threshold = baseRadius / Math.Pow(2, zoom);
            m_altitudeThresholds[zoom] = threshold * scalingFactor * zoomScale;
        }

        m_updateQuadTreeThread = new Thread(UpdateQuadTreeThreadFunction);
        m_updateQuadTreeThread.Start();
    }

    // Constantly performs BFS on the quadtree and splits/merge terrain quad tree nodes
    // based on the camera
    private void UpdateQuadTree(TerrainQuadTreeNode node)
    {
        if (node == null) return;

        float minLat = m_camera.CurrentLat - m_camera.ApproxVisibleLatRadius;
        float maxLat = m_camera.CurrentLat + m_camera.ApproxVisibleLatRadius;

        float minLon = m_camera.CurrentLon - m_camera.ApproxVisibleLonRadius;
        float maxLon = m_camera.CurrentLon + m_camera.ApproxVisibleLonRadius;

        // Don't bother checking if we are outside the visible lat/lon range of the camera
        if (node.Chunk.MapTile.Latitude < minLat || node.Chunk.MapTile.Latitude > maxLat)
        {
            return;
        }

        if (node.Chunk.MapTile.Longitude < minLon || node.Chunk.MapTile.Longitude > maxLon)
        {
            return;
        }

        CallDeferred("ShouldSplit", node);

        // Early termination if node is at correct LOD
        if (node.IsLoadedInScene && !node.ShouldSplit) return;

        // Handle splitting
        if (node.IsLoadedInScene && node.ShouldSplit)
        {
            CallDeferred("Split", node);
            return;
        }

        // Check for merging only if node is loaded
        if (node.IsLoadedInScene)
        {
            bool anyChildShouldMerge = false;
            for (int i = 0; i < 4; i++)
            {
                if (node.ChildNodes[i] != null)
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

        // Continue DFS for unloaded nodes
        if (!node.IsLoadedInScene)
        {
            for (int i = 0; i < 4; i++)
            {
                UpdateQuadTree(node.ChildNodes[i]);
            }
        }
    }

    private void UpdateQuadTreeThreadFunction()
    {
        while (true)
        {
            UpdateQuadTree(m_rootNode);
            Thread.Sleep(5000);
        }
    }

    // Initializes the quadtree to a particular zoom level.
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
            // Number of nodes in a quadtree level = 4^z, or 2^z * 2^z
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

    void GenerateChildren(TerrainQuadTreeNode parentNode)
    {
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


    private void InitializeTerrainQuadTreeNodeMesh(TerrainQuadTreeNode node)
    {
        var tile = node.Chunk.MapTile;
        // TODO(Argyraspides, 11/02/2025): Again, please, PLEASE make this WGS84 thing abstracted away too. TerrainQuadTree
        // shouldn't need to worry about this. Just generate the mesh and be done with it. Interfaces, interfaces,
        // interfaces!
        ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
            (float)tile.Latitude,
            (float)tile.Longitude,
            (float)tile.LatitudeRange,
            (float)tile.LongitudeRange
        );
        node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        node.Chunk.Name = $"TerrainChunk_z{tile.ZoomLevel}_x{tile.LongitudeTileCoo}_y{tile.LatitudeTileCoo}";
        node.Chunk.Load();
        node.IsLoadedInScene = true;
        AddChild(node.Chunk);
        node.Chunk.SetPositionAndSize();
    }

    // Determines if the input terrain quad tree node should be split or not based on the camera distance,
    // what it can see, etc.
    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node cannot be null");
        }

        if (node.Depth >= m_maxDepth)
        {
            node.ShouldSplit = false;
            return false;
        }

        double distThreshold = m_altitudeThresholds[node.Depth];
        float distToCam = node.Chunk.Position.DistanceTo(m_camera.Position) - 3000;
        // GD.Print("Current distance to camera: ", distToCam);
        node.ShouldSplit = distThreshold > distToCam;
        return node.ShouldSplit;
    }

    private bool ShouldMerge(TerrainQuadTreeNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node cannot be null");
        }

        double mergeThreshold = m_altitudeThresholds[node.Depth];
        float distToCam = node.Chunk.Position.DistanceTo(m_camera.Position) - 3000;
        bool shouldMerge = distToCam > mergeThreshold;
        node.ShouldMerge = shouldMerge;
        return shouldMerge;
    }

    // Splits the terrain quad tree node into four children, and makes its parent invisible
    private void Split(TerrainQuadTreeNode node)
    {
        node.Chunk.Visible = false;
        GenerateChildren(node);

        for (int i = 0; i < 4; i++)
        {
            InitializeTerrainQuadTreeNodeMesh(node.ChildNodes[i]);
        }
    }

    // The input parameter is the parent quadtree whose children we wish to merge into it. Works by removing the children
    // from the scene tree/making them invisible, and then toggling itself to be visible or whatever
    private void Merge(TerrainQuadTreeNode parent)
    {
        parent.Chunk.Visible = true;
        for (int i = 0; i < 4; i++)
        {
            parent.ChildNodes[i].Chunk.Visible = false;
        }
    }

    // Called whenever we exceed the max number of allowed nodes in our quadtree. Completely destroys a quadtree node
    // by removing it from the scene tree and discarding it entirely. If we ever want to load this particular node again,
    // we will have to make it from scratch and the map tile data will have to be fetched from the map tile cache which is already
    // implemented elsewhere
    private void RemoveQuadTreeNode(TerrainQuadTreeNode node)
    {
    }

    // Removes an entire subtree from the quadtree, including the parent
    private void RemoveSubQuadTree(TerrainQuadTreeNode parent)
    {
    }

    // Performs a frustum culling procedure on the quadtree and returns a queue of parent nodes
    // which contain visible children (or the parent itself is visible).
    private Queue<TerrainQuadTreeNode> PerformFrustumCulling(TerrainQuadTreeNode parent)
    {
        throw new NotImplementedException();
    }

    private sealed partial class TerrainQuadTreeNode : Node
    {
        public TerrainChunk Chunk { get; set; }
        public TerrainQuadTreeNode[] ChildNodes { get; set; }

        // Tells us if the quadtree node is loaded in the actual scene.
        // When we are traversing the quadtree, if we ever encounter something that *is* loaded
        // in the scene, then we know that this particular node is visible to the camera. It is at
        // this point we can call the functions ShouldSplit(), and Split() if we need to split,
        // and ShouldMerge() and Merge() if we need to merge
        // So this flag helps us optimize performance by not calling those functions on nodes
        // that aren't currently visible and hence don't need to be considered for splitting/merging.
        // Once we do encounter isLoadedInScene to be true, then we can also not add the children of this
        // node to our BFS, so we save on space as well
        public bool IsLoadedInScene { get; set; }
        public bool ShouldSplit { get; set; }
        public bool ShouldMerge { get; set; }

        public int Depth;

        public TerrainQuadTreeNode(TerrainChunk chunk, int depth)
        {
            Chunk = chunk;
            ChildNodes = new TerrainQuadTreeNode[4] { null, null, null, null };
            IsLoadedInScene = false;
            ShouldSplit = false;
            ShouldMerge = false;
            Depth = depth;
        }
    }
}
