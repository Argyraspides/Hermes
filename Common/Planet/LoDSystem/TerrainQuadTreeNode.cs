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


namespace Hermes.Common.Planet.LoDSystem;

using System;
using Godot;

/// <summary>
/// An individual node in a quadtree structure meant to represent TerrainChunks.
/// </summary>
public sealed partial class TerrainQuadTreeNode : Node
{
    public TerrainChunk Chunk { get; }
    public TerrainQuadTreeNode[] ChildNodes { get; } = new TerrainQuadTreeNode[4] { null, null, null, null };
    public int Depth { get; }

    // We aren't allowed to obtain the position property of nodes in the scene tree from other threads.
    // Here we store a copy of the terrain quad tree node's position and visibility (derived from TerrainChunk)
    // which are needed to determine conditions under which nodes need to be split/merged
    public Vector3 Position { get; private set; }
    public bool IsVisible { get; set; }

    public TerrainQuadTreeNode(TerrainChunk chunk, int depth)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        Depth = depth;
        AddChild(Chunk);
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public bool HasChildren()
    {
        if (ChildNodes.Length == 0) { return false; }

        for (int i = 0; i < ChildNodes.Length; i++)
        {
            if (!GodotUtils.IsValid(ChildNodes[i])) { return false; }
        }

        return true;
    }
}
