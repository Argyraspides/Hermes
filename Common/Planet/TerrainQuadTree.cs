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

        // List of child terrain chunks.
        public List<TerrainChunk> ChildChunks;

        public TerrainQuadTreeNode(TerrainChunk chunk)
        {
            Chunk = chunk;
            // We expect up to 4 children.
            ChildChunks = new List<TerrainChunk>(4);
        }
    }

    /// <summary>
    /// Each row in this list represents a zoom level, and contains all the quadtree nodes at that level.
    /// </summary>
    private List<List<TerrainQuadTreeNode>> m_terrainQuadTreeNodes { get; set; }

    /// <summary>
    /// Splits the quadtree node associated with the particular latitude/longitude.
    /// </summary>
    /// <param name="latitude">Latitude of the quadtree node to be split (in radians).</param>
    /// <param name="longitude">Longitude of the quadtree node to be split (in radians).</param>
    /// <param name="zoom">Zoom level to split at.</param>
    public void SplitQuadTree(float latitude, float longitude, int zoom)
    {
        // TODO: Create

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

        // Initialize the outer list.
        m_terrainQuadTreeNodes = new List<List<TerrainQuadTreeNode>>();

        TerrainQuadTreeNode rootNode = new TerrainQuadTreeNode(new TerrainChunk(
            new MapTile(0, 0, 0)
        ));

        var queue = new Queue<(TerrainQuadTreeNode, int parentRow, int parentCol)>();
        queue.Enqueue((rootNode, 0, 0));

        for (int currZoomLevel = 0; currZoomLevel <= maxZoom; currZoomLevel++)
        {
            // For all nodes in the queue, generate all their child nodes
            int queueSize = (int)Math.Pow(4, currZoomLevel);
            m_terrainQuadTreeNodes.Add(new List<TerrainQuadTreeNode>());

            for (int i = 0; i < queueSize; i++)
            {
                // Parent node
                (TerrainQuadTreeNode node, int parentRow, int parentCol) = queue.Dequeue();
                m_terrainQuadTreeNodes[currZoomLevel].Add(node);

                if (currZoomLevel == maxZoom)
                {
                    continue;
                }

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

                    TerrainChunk childChunk = new TerrainChunk(
                            new MapTile(childLat, childLon, currZoomLevel + 1)
                        );

                    TerrainQuadTreeNode childNode = new TerrainQuadTreeNode(childChunk);
                    node.ChildChunks.Add(childChunk);
                    queue.Enqueue((childNode, childRow, childCol));
                }
            }
        }
    }

    public List<TerrainQuadTreeNode> GetQuadTreeLevel(int zoomLevel)
    {
        if (zoomLevel >= m_terrainQuadTreeNodes.Count || zoomLevel < 0)
        {
            throw new IndexOutOfRangeException("In TerrainQuadTree: Trying to grab quadtree level that doesn't exist");
        }
        return m_terrainQuadTreeNodes[zoomLevel];
    }

    public List<TerrainQuadTreeNode> GetLastQuadTreeLevel()
    {
        return m_terrainQuadTreeNodes[m_terrainQuadTreeNodes.Count - 1];
    }

}

