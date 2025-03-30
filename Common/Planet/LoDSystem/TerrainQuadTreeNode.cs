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

namespace Hermes.Common.Planet.LoDSystem;

using System;
using Godot;
using Hermes.Common.GodotUtils;

/// <summary>
/// An individual node in a quadtree structure meant to represent TerrainChunks.
/// </summary>
public sealed partial class TerrainQuadTreeNode : Node3D
{
    public TerrainChunk Chunk { get; }
    public TerrainQuadTreeNode[] ChildNodes { get; } = new TerrainQuadTreeNode[4] { null, null, null, null };
    public int Depth { get; }

    // We aren't allowed to obtain the position property of nodes in the scene tree from other threads.
    // Here we store a copy of the terrain quad tree node's position and visibility (derived from TerrainChunk)
    // which are needed to determine conditions under which nodes need to be split/merged
    public Vector3 Position { get; set; }
    public bool IsDeepest { get; set; }

    public TerrainQuadTreeNode(TerrainChunk chunk, int depth)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        Depth = depth;
        AddChild(Chunk);
    }

    public bool HasAllChildren()
    {
        if (ChildNodes.Length == 0) { return false; }

        for (int i = 0; i < ChildNodes.Length; i++)
        {
            if (!GodotUtils.IsValid(ChildNodes[i])) { return false; }
        }

        return true;
    }
}
