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
using Godot;

/*
 TODO(Argyraspides, 09/02/2025): Current issues:

Currently there is a high-resolution texture which covers the entirety of Earth's surface
at zoom level 6 (4096 tiles) bundled with the application. At startup this is loaded in order to prevent
network requests to the backend servers

This means that at startup, we will somehow have to BFS our way to the entire leaf node level and call .Load()
on all the chunks. Since this is initialization this may not be a problem as it only occurs once per application startup, though
I'm not sure about this.

Although this true-tree representation saves on memory, it means we are going to be constantly traversing the tree and loading/unloading
chunks as the user pans around, so we will be doing BFS like possibly everytime the user is panning around and zooming in which is not ideal,
but again I don't know if this will be an issue.

 */
public partial class TerrainQuadTree : Node
{
    /// <summary>
    /// Represents a node in a terrain quadtree structure.
    /// Each node can have exactly 0 or 4 child chunks.
    /// </summary>
    public sealed class TerrainQuadTreeNode
    {
        // The TerrainChunk associated with this node.
        public TerrainChunk Chunk;
        public TerrainQuadTreeNode[] ChildNodes;

        public TerrainQuadTreeNode(TerrainChunk chunk)
        {
            Chunk = chunk;
            ChildNodes = new TerrainQuadTreeNode[4];
        }
    }

    private TerrainQuadTreeNode m_rootNode;

    private int m_currentDepth;

    private List<TerrainQuadTreeNode> m_nullIslandNodes { get; set; }

    /// <summary>
    /// Splits the quadtree node by determining lat/lon tile coordinates of its four children,
    /// then injecting the children into the parent node's ChildNodes array
    /// </summary>
    /// <param name="parentNode"> The quadtree node to be split </param>
    public void SplitQuadTreeNode(TerrainQuadTreeNode parentNode)
    {
        int parentRow = parentNode.Chunk.MapTile.LatitudeTileCoo;
        int parentCol = parentNode.Chunk.MapTile.LongitudeTileCoo;
        int currZoomLevel = parentNode.Chunk.MapTile.ZoomLevel;
        // Generate children for this node. 0 = top left, 1 = top right, 2 = bottom left, 3 = bottom right
        for (int c = 0; c < 4; c++ /* haha C++ in C# */)
        {
            // Top left
            int childRow = parentRow * 2;
            int childCol = parentCol * 2;

            // Top right or bottom right respectively
            childCol += (c == 1 || c == 3) ? 1 : 0;
            // Bottom left or bottom right respectively
            childRow += (c == 2 || c == 3) ? 1 : 0;

            // The lat of a map tile is at its center, while MapUtils gives back
            // latitude of the northern edge, so we compute half the range and subtract to get the
            // center lat
            float childLat = (float)MapUtils.MapTileToLatitude(childRow, currZoomLevel + 1);
            float childLatRange = (float)MapUtils.TileToLatRange(childRow, currZoomLevel + 1);
            float childLatHalfRange = childLatRange / 2;
            childLat -= childLatHalfRange;

            // The lat of a map tile is at its center, while the MapUtils gives back
            // longitude of the western edge, so we compute half the range and subtract to get the
            // center lon
            float childLon = (float)MapUtils.MapTileToLongitude(childCol, currZoomLevel + 1);
            float childLonRange = (float)MapUtils.TileToLonRange(currZoomLevel + 1);
            float childLonHalfRange = childLonRange / 2;
            childLon += childLonHalfRange;

            // Create child node and give it to our parent
            MapTile childMapTile = new MapTile(childLat, childLon, currZoomLevel + 1);
            TerrainChunk childChunk = new TerrainChunk(childMapTile);
            TerrainQuadTreeNode childNode = new TerrainQuadTreeNode(childChunk);
            parentNode.ChildNodes[c] = childNode;
        }
    }

    /// <summary>
    /// Finds a quadtree node based on latitude, longitude, and zoom level with DFS and splits it
    /// into four pieces
    /// </summary>
    /// <param name="latitude">Latitude of the quadtree to split in radians</param>
    /// <param name="longitude">Longitude of the quadtree to split in radians</param>
    /// <param name="zoom">Zoom level of the quadtree node to split (also represents the level of the quadtree itself)</param>
    public void SplitQuadTreeNodeDFS(float latitude, float longitude, int zoom)
    {
        TerrainQuadTreeNode node = FindQuadTreeNodeDFS(latitude, longitude, zoom);
        SplitQuadTreeNode(node);
    }

    public void SplitAndLoadQuadTreeNode(float latitude, float longitude, int zoom)
    {
        TerrainQuadTreeNode parent = FindQuadTreeNodeDFS(latitude, longitude, zoom);
        SplitQuadTreeNode(parent);

        parent.Chunk.ToggleVisible(false);

        for (int c = 0; c < 4; c++)
        {
            ProcessLeafNode(parent.ChildNodes[c]);
        }
    }

    /// <summary>
    /// Finds a quadtree node at a specific latitude, longitude, and zoom level using the DFS algorithm.
    /// No stack is allocated for DFS.
    /// </summary>
    /// <param name="latitude">Latitude in radians</param>
    /// <param name="longitude">Longitude in radians</param>
    /// <param name="zoom">Zoom level of the quadtree node to split (also represents the level of the quadtree itself)</param>
    /// <returns>The located quadtree node</returns>
    public TerrainQuadTreeNode FindQuadTreeNodeDFS(float latitude, float longitude, int zoom)
    {
        if (zoom == 0) { return m_rootNode; }

        TerrainQuadTreeNode currentNode = m_rootNode;

        for (int i = 1; i <= zoom - 1; i++)
        {
            int currLatTileCoo = MapUtils.LatitudeToTileCoordinateMercator(latitude, i);
            int currLonTileCoo = MapUtils.LongitudeToTileCoordinateMercator(longitude, i);

            // Latitude and longitude tile coordinates of where the corresponding tile would be
            // the next level down
            int nextLatTileCoo = MapUtils.LatitudeToTileCoordinateMercator(latitude, i + 1);
            int nextLonTileCoo = MapUtils.LongitudeToTileCoordinateMercator(longitude, i + 1);

            // (2x, 2y) = Top right corner
            if (nextLatTileCoo == currLatTileCoo * 2 && nextLonTileCoo == currLonTileCoo * 2)
            {
                currentNode = m_rootNode.ChildNodes[0];
            }
            // (2x + 1, 2y) = Top left corner
            else if (nextLatTileCoo == currLatTileCoo * 2 + 1 && nextLonTileCoo == currLonTileCoo * 2)
            {
                currentNode = m_rootNode.ChildNodes[1];
            }
            // (2x, 2y + 1) = Bottom left corner
            else if (nextLatTileCoo == currLatTileCoo * 2 && nextLonTileCoo == currLonTileCoo * 2 + 1)
            {
                currentNode = m_rootNode.ChildNodes[2];
            }
            // (2x + 1, 2y + 1) = Bottom right corner
            else if (nextLatTileCoo == currLatTileCoo * 2 + 1 && nextLonTileCoo == currLonTileCoo * 2 + 1)
            {
                currentNode = m_rootNode.ChildNodes[3];
            }
        }

        return currentNode;
    }

    /// <summary>
    /// Initializes a quadtree of the Earth up to a maximum zoom level using BFS.
    /// Each level of the quadtree is processed completely before moving on to the next.
    /// </summary>
    /// <param name="maxZoom">The maximum zoom level (depth) of the quadtree.</param>
    public void InitializeQuadTree(int maxZoom)
    {
        m_nullIslandNodes = new List<TerrainQuadTreeNode>();

        // Create the root node (zoom level 0).
        m_rootNode = new TerrainQuadTreeNode(new TerrainChunk(new MapTile(0, 0, 0)));

        // Queue for BFS: each entry holds a node and its grid coordinates.
        var nodeQueue = new Queue<(TerrainQuadTreeNode node, int row, int col)>();
        nodeQueue.Enqueue((m_rootNode, 0, 0));

        // Build the quadtree level by level.
        for (int currZoomLevel = 0; currZoomLevel < maxZoom; currZoomLevel++)
        {
            // The children will be at zoom level (currZoomLevel + 1)
            int childZoom = currZoomLevel + 1;
            // At the current level there are exactly 4^currZoomLevel nodes.
            int nodesAtCurrentLevel = (int)Math.Pow(4, currZoomLevel);

            for (int i = 0; i < nodesAtCurrentLevel; i++)
            {
                var (parentNode, parentRow, parentCol) = nodeQueue.Dequeue();

                // Create child nodes for each quadrant (0 = top-left, 1 = top-right,
                // 2 = bottom-left, 3 = bottom-right)
                foreach (var (rowOffset, colOffset, quadrantIndex) in GetChildQuadrants())
                {
                    int childRow = parentRow * 2 + rowOffset;
                    int childCol = parentCol * 2 + colOffset;

                    // Compute the center coordinates of the child tile.
                    float childLat = (float)MapUtils.ComputeCenterLatitude(childRow, childZoom);
                    float childLon = (float)MapUtils.ComputeCenterLongitude(childCol, childZoom);

                    // Create the map tile, terrain chunk and quadtree node.
                    var childMapTile = new MapTile(childLat, childLon, childZoom);
                    var childChunk = new TerrainChunk(childMapTile);
                    var childNode = new TerrainQuadTreeNode(childChunk);
                    parentNode.ChildNodes[quadrantIndex] = childNode;

                    // Enqueue the child node for further subdivision.
                    nodeQueue.Enqueue((childNode, childRow, childCol));

                    // Check if this node represents "null island" (i.e. (0,0) center).
                    int midIndex = (1 << childZoom) / 2;
                    if (childRow == midIndex && childCol == midIndex)
                    {
                        m_nullIslandNodes.Add(childNode);
                    }
                }
            }
        }

        // Process remaining nodes in the queue (leaf nodes).
        while (nodeQueue.Count > 0)
        {
            var (node, _, _) = nodeQueue.Dequeue();
            ProcessLeafNode(node);
        }

        m_currentDepth = maxZoom;
    }

    /// <summary>
    /// Returns the row and column offsets for each child quadrant.
    /// </summary>
    private IEnumerable<(int rowOffset, int colOffset, int quadrantIndex)> GetChildQuadrants()
    {
        // Order: top-left, top-right, bottom-left, bottom-right.
        yield return (0, 0, 0);
        yield return (0, 1, 1);
        yield return (1, 0, 2);
        yield return (1, 1, 3);
    }


    /// <summary>
    /// Processes a leaf node by generating its mesh and loading the terrain chunk.
    /// </summary>
    private void ProcessLeafNode(TerrainQuadTreeNode node)
    {
        var tile = node.Chunk.MapTile;
        ArrayMesh meshSegment = WGS84EllipsoidMeshGenerator.CreateEllipsoidMeshSegment(
            (float)tile.Latitude,
            (float)tile.Longitude,
            (float)tile.LatitudeRange,
            (float)tile.LongitudeRange
        );

        node.Chunk.MeshInstance = new MeshInstance3D { Mesh = meshSegment };
        node.Chunk.Name = $"TerrainChunk_z{tile.ZoomLevel}_x{tile.LongitudeTileCoo}_y{tile.LatitudeTileCoo}";
        node.Chunk.Load();
        AddChild(node.Chunk);
    }


    public TerrainQuadTreeNode GetCenter(int zoomLevel)
    {
        if (zoomLevel >= m_nullIslandNodes.Count)
        {
            throw new IndexOutOfRangeException();
        }

        return m_nullIslandNodes[zoomLevel];
    }
}
