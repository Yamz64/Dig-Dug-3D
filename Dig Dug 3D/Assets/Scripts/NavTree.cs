using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavTree : MonoBehaviour
{
    public bool draw_connections, draw_chunk_borders, draw_near_player;
    public float min_nav_distance;
    public Vector3Int nav_tree_dimensions;     //dimensions of the entire nav tree in chunks

    private List<NavChunk> nav_chunks;

    //class represents a single navigation node
    public class NavNode
    {
        //CONSTRUCTOR
        public NavNode() { connections = new HashSet<(NavNode, float)>(); }

        public Vector3 position;                        //position of this node in the world
        public HashSet<(NavNode, float)> connections;   //connections this node has to other nodes along with the distance
    }
    //class helps divvy up navigation nodes to help in processing
    public class NavChunk
    {
        private int index;
        private Vector3 position;
        private Vector3Int nav_tree_dimensions;
        private List<NavNode> nav_nodes;
        private NavTree nav_tree;

        public NavChunk(int i, Vector3Int n_tree_dim, NavTree tree, Vector3 p) {
            index = i;
            nav_nodes = new List<NavNode>();
            nav_tree_dimensions = n_tree_dim;
            nav_tree = tree;
            position = p;
        }

        public List<NavNode> GetNodes() { return nav_nodes; }

        public void DrawConnections()
        {
            for(int i=0; i<nav_nodes.Count; i++)
            {
                foreach((NavNode, float) connected in nav_nodes[i].connections)
                    Debug.DrawRay(nav_nodes[i].position, connected.Item1.position - nav_nodes[i].position, Color.green);
            }
        }

        public void DrawBorders()
        {
            Vector3 map_dimensions = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<TerrainGeneration>().GetMapDimensions();
            Vector3 chunk_bounds = new Vector3(map_dimensions.x / nav_tree_dimensions.x, map_dimensions.y / nav_tree_dimensions.y, map_dimensions.z / nav_tree_dimensions.z);
            chunk_bounds /= 2.0f;
            ExtDebug.DrawBox(position, chunk_bounds, Quaternion.identity, Color.white);
        }

        //function attempts to add a node, if it is too close to an adjacent node, it will fail returning false
        public bool AddNode(NavNode node)
        {
            for(int i=0; i<nav_nodes.Count; i++)
            {
                if (Vector3.Distance(node.position, nav_nodes[i].position) < nav_tree.min_nav_distance)
                    return false;
            }
            nav_nodes.Add(node);
            return true;
        }
        
        //Function helps in getting the index of the adjacent chunk, returns -1 if no such chunk exists
        public int GetAdjacentChunk(Vector3Int direction)
        {
            //error checking
            if (direction.x != -1 && direction.x != 0 && direction.x != 1)
                return -1;
            if (direction.y != -1 && direction.y != 0 && direction.y != 1)
                return -1;
            if (direction.z != -1 && direction.z != 0 && direction.z != 1)
                return -1;

            int adjacent_chunk = index;
            int cross_section = nav_tree_dimensions.z * nav_tree_dimensions.y;

            adjacent_chunk += direction.x * cross_section;
            adjacent_chunk += nav_tree_dimensions.z * direction.y;
            adjacent_chunk += direction.z;

            if (adjacent_chunk < 0 || adjacent_chunk >= nav_tree_dimensions.x * nav_tree_dimensions.y * nav_tree_dimensions.z)
                adjacent_chunk = -1;

            return adjacent_chunk;
        }

        //function will attempt to make a connection between 2 nodes
        void MakeConnection(NavNode a, NavNode b)
        {
            Vector3 connection_dir = b.position - a.position;
            //if nothing blocks this ray, then make a connection
            if(!Physics.BoxCast(a.position, Vector3.one * 0.5f, connection_dir, Quaternion.identity, connection_dir.magnitude, LayerMask.GetMask("Terrain")))
            {
                a.connections.Add((b, Vector3.Distance(a.position, b.position)));
                b.connections.Add((a, Vector3.Distance(a.position, b.position)));
            }
        }

        //Function regenerates all connections between this chunk's nodes and to an adjacent chunk's nodes
        public void RegenerateConnections()
        {
            //first clear all connections between the nodes of this chunk with those of adjacent chunks
            for(int i = 0; i<nav_nodes.Count; i++)
                nav_nodes[i].connections.Clear();

            //now generate new connections between the nodes of this chunk
            for(int i = 0; i<nav_nodes.Count; i++)
            {
                if (i != nav_nodes.Count - 1) {
                    for (int j = i + 1; j < nav_nodes.Count; j++)
                    {
                        MakeConnection(nav_nodes[i], nav_nodes[j]);
                    }
                }
            }

            //finally, get a list of all adjacent nodes and make connections between the nodes of this chunk with those chunks
            for (int i = 0; i < nav_nodes.Count; i++)
            {
                for(int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        for (int z = -1; z < 2; z++)
                        {
                            if (x == 0 && y == 0 && z == 0)
                                continue;

                            int adjacent_chunk = GetAdjacentChunk(new Vector3Int(x, y, z));
                            if (adjacent_chunk != -1)
                                for (int j = 0; j < nav_tree.nav_chunks[adjacent_chunk].nav_nodes.Count; j++)
                                    MakeConnection(nav_nodes[i], nav_tree.nav_chunks[adjacent_chunk].nav_nodes[j]);
                        }
                    }
                }
            }
        }

        //Driver function to get all connected nodes to a node
        public List<NavNode> GetAllConnectedNodes(int index)
        {
            NavNode starting_node = nav_nodes[index];
            List<NavNode> connected_nodes = new List<NavNode>();
            GetAllConnectedNodes(starting_node, ref connected_nodes);
            return connected_nodes;
        }

        //Recursive function gets all nodes connected to this node
        void GetAllConnectedNodes(NavNode node, ref List<NavNode> connected_nodes)
        {
            //first see if this node is already in connected_nodes, if it is then abort
            if (connected_nodes.Contains(node))
                return;

            //add this node
            connected_nodes.Add(node);

            //now examine this node's connections
            foreach ((NavNode, float) connected in node.connections)
                GetAllConnectedNodes(connected.Item1, ref connected_nodes);
        }
    }

    //function calculates the chunkspace position of a point
    public Vector3Int WorldspaceToChunkspace(Vector3 position)
    {
        Vector3 map_dimensions = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<TerrainGeneration>().GetMapDimensions();

        //do some error checking to see if this nav node is outside of the bounds of the map
        Bounds map_bounds = new Bounds();
        map_bounds.size = map_dimensions;

        if (!map_bounds.Contains(position))
        {
            Debug.LogWarning($"Could not convert worldspace position: {position}, to chunkspace aborting...");
            return -Vector3Int.one;
        }

        //first off, get the back bottom left corner of the map
        Vector3 back_bottom_left = new Vector3(-map_dimensions.x / 2.0f, -map_dimensions.y / 2.0f, -map_dimensions.z / 2.0f);
        back_bottom_left.x -= back_bottom_left.x % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.y -= back_bottom_left.y % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.z -= back_bottom_left.z % 2 == 0 ? 0 : 0.5f;

        //now calculate the chunkspace position of this node
        Vector3 chunk_space = position - back_bottom_left;
        chunk_space.x /= map_dimensions.x;
        chunk_space.y /= map_dimensions.y;
        chunk_space.z /= map_dimensions.z;

        chunk_space.x *= nav_tree_dimensions.x;
        chunk_space.y *= nav_tree_dimensions.y;
        chunk_space.z *= nav_tree_dimensions.z;

        Vector3Int chunk_space_int = new Vector3Int((int)chunk_space.x, (int)chunk_space.y, (int)chunk_space.z);

        chunk_space_int.x = chunk_space_int.x >= nav_tree_dimensions.x ? nav_tree_dimensions.x - 1 : chunk_space_int.x;
        chunk_space_int.y = chunk_space_int.y >= nav_tree_dimensions.y ? nav_tree_dimensions.y - 1 : chunk_space_int.y;
        chunk_space_int.z = chunk_space_int.z >= nav_tree_dimensions.z ? nav_tree_dimensions.z - 1 : chunk_space_int.z;

        return chunk_space_int;
    }

    //Function gets the worldspace position given the chunkspace
    public Vector3 ChunkspaceToWorldSpace(Vector3Int position)
    {
        //error checking
        if (position.x < 0 || position.y < 0 || position.z < 0 ||
            position.x >= nav_tree_dimensions.x || position.y >= nav_tree_dimensions.y || position.z >= nav_tree_dimensions.z)
        {
            Debug.LogWarning($"{position} cannot be converted to worldspace as it does not lie within the confines of the map!, aborting...");
            return -Vector3.one;
        }

        //get the back bottom left corner of the map
        Vector3 map_dimensions = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<TerrainGeneration>().GetMapDimensions();
        Vector3 back_bottom_left = new Vector3(-map_dimensions.x / 2.0f, -map_dimensions.y / 2.0f, -map_dimensions.z / 2.0f);
        back_bottom_left.x -= back_bottom_left.x % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.y -= back_bottom_left.y % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.z -= back_bottom_left.z % 2 == 0 ? 0 : 0.5f;

        //offset by half the chunk size
        Vector3 chunk_dimensions = new Vector3(map_dimensions.x / nav_tree_dimensions.x, map_dimensions.y / nav_tree_dimensions.y, map_dimensions.z / nav_tree_dimensions.z);
        back_bottom_left += chunk_dimensions * 0.5f;

        //calculate the position now
        return back_bottom_left + new Vector3(position.x * chunk_dimensions.x, position.y * chunk_dimensions.y, position.z * chunk_dimensions.z);
    }

    //Function gets the chunk that a position lies in
    public NavChunk GetChunk(Vector3 position)
    {
        Vector3Int chunk_space_pos = WorldspaceToChunkspace(position);

        //do some error checking
        if (chunk_space_pos == -Vector3Int.one)
        {
            Debug.LogWarning($"Map does not contain nav node: {position}, aborting...");
            return null;
        }

        int chunk_index = chunk_space_pos.z + nav_tree_dimensions.z * chunk_space_pos.y + nav_tree_dimensions.z * nav_tree_dimensions.y * chunk_space_pos.x;
        return nav_chunks[chunk_index];
    }

    //Function will get the closest node to a point
    public NavNode GetClosestNodeToPoint(Vector3 position, ref int index)
    {
        //First convert this position to chunkspace
        Vector3Int chunk_space_pos = WorldspaceToChunkspace(position);

        //do some error checking
        if (chunk_space_pos == -Vector3Int.one)
        {
            Debug.LogWarning($"Map does not contain nav node: {position}, aborting...");
            return null;
        }

        //calculate the index of this chunk, then iterate through all of the nodes in this chunk, comparing which one is the closest
        int chunk_index = chunk_space_pos.z + nav_tree_dimensions.z * chunk_space_pos.y + nav_tree_dimensions.z * nav_tree_dimensions.y * chunk_space_pos.x;

        int shortest_index = -1;
        float shortest_distance = Mathf.Infinity;
        for(int i = 0; i<nav_chunks[chunk_index].GetNodes().Count; i++)
        {
            if(Vector3.Distance(nav_chunks[chunk_index].GetNodes()[i].position, position) < shortest_distance)
            {
                shortest_distance = Vector3.Distance(nav_chunks[chunk_index].GetNodes()[i].position, position);
                shortest_index = i;
            }
        }

        //return null if no node was found within this chunk
        if (shortest_index == -1)
            return null;

        index = shortest_index;
        return nav_chunks[chunk_index].GetNodes()[shortest_index];
    }

    //function will add a node to this tree given a position
    public void AddNode(Vector3 position)
    {
        Vector3Int chunk_space_int = WorldspaceToChunkspace(position);

        //do some error checking
        if(chunk_space_int == -Vector3Int.one)
        {
            Debug.LogWarning($"Map does not contain nav node: {position}, aborting...");
            return;
        }

        //calculate the index of this chunk, create a new node with no connections, add it to that chunk and recalculate that chunk's connections
        int chunk_index = chunk_space_int.z + nav_tree_dimensions.z * chunk_space_int.y + nav_tree_dimensions.z * nav_tree_dimensions.y * chunk_space_int.x;

        NavNode node = new NavNode();
        node.position = position;
        if(nav_chunks[chunk_index].AddNode(node))
            nav_chunks[chunk_index].RegenerateConnections();
    }

    // Start is called before the first frame update
    void Awake()
    {
        int chunk_index = 0;
        nav_chunks = new List<NavChunk>();
        //Start by initializing all of the nav chunks
        for(int x = 0; x<nav_tree_dimensions.x; x++)
        {
            for(int y = 0; y<nav_tree_dimensions.y; y++)
            {
                for(int z = 0; z < nav_tree_dimensions.z; z++)
                {
                    nav_chunks.Add(new NavChunk(chunk_index, nav_tree_dimensions, this, ChunkspaceToWorldSpace(new Vector3Int(x, y, z))));
                    chunk_index++;
                }
            }
        }
    }

    private void Update()
    {
        if (draw_connections)
        {
            if (!draw_near_player)
            {
                for (int i = 0; i < nav_chunks.Count; i++)
                {
                    nav_chunks[i].DrawConnections();
                }
            }
            else
            {
                NavChunk player_chunk = GetChunk(GameObject.FindGameObjectWithTag("Player").transform.position);

                player_chunk.DrawConnections();

                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        for (int z = -1; z < 2; z++)
                        {
                            if (x == 0 && y == 0 && z == 0)
                                continue;
                            int adjacent_chunk = player_chunk.GetAdjacentChunk(new Vector3Int(x, y, z));
                            nav_chunks[adjacent_chunk].DrawConnections();
                        }
                    }
                }
            }
        }
        if (draw_chunk_borders)
        {
            if (!draw_near_player)
            {
                for (int i = 0; i < nav_chunks.Count; i++)
                {
                    nav_chunks[i].DrawBorders();
                }
            }
            else
            {
                NavChunk player_chunk = GetChunk(GameObject.FindGameObjectWithTag("Player").transform.position);

                player_chunk.DrawBorders();

                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        for (int z = -1; z < 2; z++)
                        {
                            if (x == 0 && y == 0 && z == 0)
                                continue;
                            int adjacent_chunk = player_chunk.GetAdjacentChunk(new Vector3Int(x, y, z));
                            nav_chunks[adjacent_chunk].DrawBorders();
                        }
                    }
                }
            }
        }
    }
}
