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


using Godot;
using System;
using System.Collections.Generic;

/*
 TODO(Argyraspides, 09/02/2025): Current issues:

 The terrain quad tree works but its implemented pretty poorly IMO. Right now, the quad tree is represented as a
 2D array where each row represents a particular level, containing the entire level of quadtree nodes.

 Suppose we zoom in and are looking at a section of a planet which should belong, say, in the middle of a level
 then we will need to create (2^zoom / 2) quadtree nodes to accomodate for this request which is unreasonable as
 we aren't even looking at all those other ones anyway and hence they shouldn't exist.

 This means in the worst case, at zoom level 20, we could potentially have up to:

 (2^0 * 2^0) + (2^1 * 2^1) + (2^2 * 2^2) ... + (2^20 * 2^20) nodes in the quadtree, even
 though the user may have only been looking at a very specific region of the Earth and never
 looked anywhere else.

 Change the quadtree implementation so that it is a true tree structure that looks like:

   public sealed class TerrainQuadTreeNode
   {
       // The TerrainChunk associated with this node.
       public TerrainChunk Chunk;
       // List of child terrain chunks.
       public List<TerrainQuadTreeNode> ChildNodes;
       public TerrainQuadTreeNode(TerrainChunk chunk)
       {
           Chunk = chunk;
           ChildNodes = new List<TerrainQuadTreeNode>(4);
       }
   }

Though the thing is, currently there is a high-resolution texture which covers the entirety of Earth's surface
at zoom level 6 (4096 tiles) bundled with the application. At startup this is loaded in order to prevent
network requests to the backend servers

This means that at startup, we will somehow have to BFS our way to the entire leaf node level and call .Load()
on all the chunks. Since this is initialization this may not be a problem as it only occurs once per application startup, though
I'm not sure about this.

Although this true-tree representation saves on memory, it means we are going to be constantly traversing the tree and loading/unloading
chunks as the user pans around, so we will be doing BFS like possibly everytime the user is panning around and zooming in which is not ideal,
but again I don't know if this will be an issue.

 */
public class TerrainQuadTree
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

    private List<TerrainQuadTreeNode> m_nullIslandNodes { get; set; }

    /// <summary>
    /// Splits the quadtree node associated with the particular latitude/longitude.
    /// </summary>
    /// <param name="latitude">Latitude of the quadtree node to be split (in radians).</param>
    /// <param name="longitude">Longitude of the quadtree node to be split (in radians).</param>
    /// <param name="zoom">Zoom level to split at.</param>
    public void SplitQuadTree(float latitude, float longitude, int zoom)
    {
    }

    /// <summary>
    /// Initializes a quadtree of the Earth up to a maximum zoom level using BFS.
    /// Each level of the quadtree is processed completely before moving on to the next.
    ///
    /// All planets are represented as a grid of tiles. Every successive zoom level, the
    /// X and Y axes split by two, meaning each side will have twice as many tiles as before.
    /// The tiles are indexed such that the top left is (0,0) and the bottom right is
    /// (2^zoom - 1, 2^zoom - 1). This means that when a tile (x, y) is split, the
    /// new coordinate of its top left tile will be (2x, 2y). Hence its top right
    /// will be (2x + 1, 2y), bottom left (2x, 2y + 1) and bottom right (2x + 1, 2y + 1)
    ///
    /// </summary>
    /// <param name="maxZoom">The maximum zoom level (depth) of the quadtree.</param>
    public void InitializeQuadTree(int maxZoom)
    {
        m_nullIslandNodes = new List<TerrainQuadTreeNode>();
        m_rootNode = new TerrainQuadTreeNode(new TerrainChunk(
            new MapTile(0, 0, 0)
        ));

        var queue = new Queue<(TerrainQuadTreeNode, int parentRow, int parentCol)>();
        queue.Enqueue((m_rootNode, 0, 0));

        for (int currZoomLevel = 1; currZoomLevel < maxZoom; currZoomLevel++)
        {
            // For all nodes in the queue, generate all their child nodes
            int queueSize = (int)Math.Pow(4, currZoomLevel);
            for (int i = 0; i < queueSize; i++)
            {
                (TerrainQuadTreeNode parentNode, int parentRow, int parentCol) = queue.Dequeue();

                // Generate children for this node
                for (int c = 0; c < 4; c++ /* haha C++ in C# */)
                {
                    // Top left
                    int childRow = parentRow * 2;
                    int childCol = parentCol * 2;

                    if (c == 1) // Top right
                    {
                        childCol++;
                    }
                    else if (c == 2) // Bottom left
                    {
                        childRow++;
                    }
                    else if (c == 3) // Bottom right
                    {
                        childCol++;
                        childRow++;
                    }


                    float childLat = (float)MapUtils.MapTileToLatitude(childRow, currZoomLevel + 1);
                    float childLatRange = (float)MapUtils.TileToLatRange(childRow, currZoomLevel + 1);
                    float childLatHalfRange = childLatRange / 2;
                    childLat -= childLatHalfRange;

                    float childLon = (float)MapUtils.MapTileToLongitude(childCol, currZoomLevel + 1);
                    float childLonRange = (float)MapUtils.TileToLonRange(currZoomLevel + 1);
                    float childLonHalfRange = childLonRange / 2;
                    childLon += childLonHalfRange;

                    MapTile childMapTile = new MapTile(childLat, childLon, currZoomLevel + 1);
                    TerrainChunk childChunk = new TerrainChunk(childMapTile);
                    TerrainQuadTreeNode childNode = new TerrainQuadTreeNode(childChunk);
                    parentNode.ChildNodes[c] = childNode;

                    queue.Enqueue((childNode, childRow, childCol));

                    if (childRow == (1 << (currZoomLevel + 1)) / 2)
                    {
                        if (childCol == (1 << (currZoomLevel + 1)) / 2)
                        {
                            m_nullIslandNodes.Add(childNode);
                        }
                    }
                }
            }
        }
    }

    public List<TerrainQuadTreeNode> GetQuadTreeLevel(int zoomLevel)
    {
        throw new NotImplementedException();
    }

    public TerrainQuadTreeNode GetCenter(int zoomLevel)
    {
        if (zoomLevel >= m_nullIslandNodes.Count)
        {
            throw new IndexOutOfRangeException();
        }

        return m_nullIslandNodes[zoomLevel];
    }

    public List<TerrainQuadTreeNode> GetLastQuadTreeLevel()
    {
        throw new NotImplementedException();
    }
}
