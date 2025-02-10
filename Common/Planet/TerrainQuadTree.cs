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
 TODO(Argyraspides, 10/02/2025):

Here's how I think this should work:

Note that when I say "split", I just mean loading up children and making the parent invisible. Remember we want to make things as smooth as
possible for the user, so as they zoom out, we arent just discarding the entire tree below the zoom level that represents the map tiles
the camera can see. Conversely, we aren't discarding the entire tree above us as we zoom in, either (in fact this would make it completely
un-traversable anyways as now we have no root to start BFS from). This makes it so that if the user is zooming in/out a lot, we can quickly
load/unload chunks as they are still in the tree, we just need to enable/disable visiblity or something equivalent. Thus, we are simply
enabling/disabling parents and children as we go along. If we ever exceed our maximum allowed nodes, then we can start considering culling parts of the tree. We must always cull downwards, as
culling upwards will break the tree (potentially removing the root node) and we cannot traverse it anymore.

- Every "game" loop, we will traverse the entire quadtree via BFS. For every node we encounter, we will check:

    - Is the node currently loaded in the scene tree? If so, we can check if it needs to be split/merged. If it doesn't need to be split,
    we can just leave it. If it doesn't need to be split, AND we have exceeded the maximum amount of nodes, then we can cut off the entire
    sub-tree below this node as they currently aren't visible anyways, and we need to clean up.
        - If we do need to split, then we can just split, and disable the parent.

    - Is the node a leaf node and NOT loaded in the scene tree? If so, check the maximum amount of allowed nodes. If
    we have exceeded it, then remove the node entirely as it is not needed.

This is all I can think of in terms of removing unnecessary nodes. This will also make tree traversal more efficient as we have a cap on
the maximum number of nodes.

Since this is going to be within some Planet class (planets will have a quadtree that they will use to manage their
surface LoD system), then TerrainQuadTree must be part of the main Godot scene tree and thus must be a part of the main thread.
The constant BFS across every loop warrants this particular function loop to be offloaded to another thread. Therefore,
the UpdateQuadTree() should run in a separate thread.

The way this will be used is:

- A planet class will have this quadtree class instance
- A planet will give the quadtree the game camera
- The terrain quad tree constantly updates based on the camera
- The planet class is happy as all LoD and terrain management is done by the terrainquadtree,
meanwhile the planet can focus on other stuff like cool atmospheric effects, orbits, etc.

 */
public partial class TerrainQuadTree : Node
{
    private Camera3D m_camera;
    private TerrainQuadTreeNode m_rootNode;

    // Maximum amount of nodes allowed in our quadtree until
    // unused nodes are culled
    private long m_maxNodes;

    TerrainQuadTree(Camera3D camera, int maxNodes = (1 << 10))
    {
        m_camera = camera;
    }

    // Constantly performs BFS on the quadtree and splits/merge terrain quad tree nodes
    // based on the camera
    public void UpdateQuadTree(Camera3D camera3D)
    {
    }

    // Initializes the quadtree to a particular zoom level.
    public void InitializeQuadTree(int zoomLevel)
    {
    }

    // Determines if the input terrain quad tree node should be split or not based on the camera distance,
    // what it can see, etc.
    private bool ShouldSplit(TerrainQuadTreeNode node)
    {
        return false;
    }

    // Splits the terrain quad tree node into four children, and makes its parent invisible
    private void Split(TerrainQuadTreeNode node)
    {
    }

    // The input parameter is the parent quadtree whose children we wish to merge into it. Works by removing the children
    // from the scene tree/making them invisible, and then toggling itself to be visible or whatever
    private void Merge(TerrainQuadTreeNode parent)
    {
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

    private sealed class TerrainQuadTreeNode
    {
        public TerrainChunk Chunk { get; set; }
        public TerrainQuadTreeNode[] ChildNodes { get; set; }
        public TerrainQuadTreeNode ParentNode { get; set; }

        // Tells us if the quadtree node is loaded in the actual scene.
        // When we are traversing the quadtree, if we ever encounter something that *is* loaded
        // in the scene, then we know that this particular node is visible to the camera. It is at
        // this point we can call the functions ShouldSplit(), and Split() if we need to split,
        // and ShouldMerge() and Merge() if we need to merge
        // So this flag helps us optimize performance by not calling those functions on nodes
        // that aren't currently visible and hence don't need to be considered for splitting/merging.
        // Once we do encounter isLoadedInScene to be true, then we can also not add the children of this
        // node to our BFS, so we save on space as well
        public bool isLoadedInScene { get; set; }

        public TerrainQuadTreeNode(TerrainChunk chunk)
        {
            Chunk = chunk;
            ChildNodes = new TerrainQuadTreeNode[4];
            isLoadedInScene = false;
        }
    }
}
