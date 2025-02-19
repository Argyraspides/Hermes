using System;
using Godot;

namespace Hermes.Common.Planet.LoDSystem;

public sealed partial class TerrainQuadTreeNode : Node
{
    public TerrainChunk Chunk { get; }
    public TerrainQuadTreeNode[] ChildNodes { get; } = new TerrainQuadTreeNode[4];
    public bool IsLoadedInScene { get; set; }
    public int Depth { get; }

    // We aren't allowed to obtain the position property of nodes in the scene tree from other threads.
    // Here we store a copy of the terrain quad tree node's position (derived from TerrainChunk).
    public Vector3 Position { get; private set; }

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
}
