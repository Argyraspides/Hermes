using Godot;
using System;
using System.Collections.Generic;

public class TerrainQuadTree
{
    /// <summary>
    /// Represents a node in a terrain quadtree structure.
    /// Each node can have exactly 0 or 4 child chunks.
    /// </summary>
    public class TerrainQuadTreeNode
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
    /// Each element in this list represents a zoom level, and contains all the quadtree nodes at that level.
    /// </summary>
    public List<List<TerrainQuadTreeNode>> TerrainQuadTreeNodes;

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
    /// </summary>
    /// <param name="maxZoom">The maximum zoom level (depth) of the quadtree.</param>
    public void InitializeQuadTree(int maxZoom)
    {

        // Initialize the outer list.
        TerrainQuadTreeNodes = new List<List<TerrainQuadTreeNode>>();

        TerrainQuadTreeNode rootNode = new TerrainQuadTreeNode(new TerrainChunk(
            0,
            0,
            (float)Math.PI,
            (float)(2.0 * Math.PI)
        ));

        var queue = new Queue<(TerrainQuadTreeNode, int parentRow, int parentCol)>();
        queue.Enqueue((rootNode, 0, 0));

        for (int currZoomLevel = 0; currZoomLevel < maxZoom; currZoomLevel++)
        {
            // For all nodes in the queue, generate all their child nodes
            int queueSize = (int)Math.Pow(4, currZoomLevel);
            TerrainQuadTreeNodes.Add(new List<TerrainQuadTreeNode>());

            for (int i = 0; i < queueSize; i++)
            {
                // Parent node
                (TerrainQuadTreeNode node, int parentRow, int parentCol) = queue.Dequeue();
                TerrainQuadTreeNodes[currZoomLevel].Add(node);

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
                            childLat,
                            childLon,
                            childLatRange,
                            childLonRange,
                            currZoomLevel + 1
                        );

                    TerrainQuadTreeNode childNode = new TerrainQuadTreeNode(childChunk);
                    node.ChildChunks.Add(childChunk);
                    queue.Enqueue((childNode, childRow, childCol));
                }
            }
        }
    }
}

