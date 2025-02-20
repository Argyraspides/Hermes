using System;
using Godot;

namespace Hermes.Common.Planet.LoDSystem;

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
