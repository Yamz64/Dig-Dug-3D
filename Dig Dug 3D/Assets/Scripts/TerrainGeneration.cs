using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    public bool slow;
    public int erode_radius;
    public Vector3Int erode_location;
    public Vector3Int chunk_voxel_dim;  //dimension of an individual chunk in voxels
    public Vector3Int chunk_dim;        //dimension of the whole map in chunks
    public float voxel_size, cave_erosion_error;
    public Material terrain_mat;
    private bool finished_generating, force_load;
    private List<Chunk> chunks;         //holds chunk data of n size with bottom left being chunk 0, and top right being chunk n - 1
                                        //sorted by X then Y then Z
    private NavTree nav_tree;
    [SerializeField]
    GameObject rock;
    [SerializeField]
    GameObject[] enemies;

    public bool GetFinished() { return finished_generating; }

    public void ForceLoadChunks() { force_load = true; }

    public void ForceRenderChunks()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (GameManager.instance != null)
                transform.GetChild(i).GetComponent<MeshRenderer>().material = GameManager.instance.GetGroundMaterial();
            transform.GetChild(i).GetComponent<MeshRenderer>().enabled = true;
            transform.GetChild(i).GetComponent<MeshCollider>().enabled = true;
        }
    }

    //class holds terrain data inside a chunk to reduce overhead
    class Chunk
    {
        public BitArray[,] terrain_data;
        public GameObject chunk_GO;
        private int index;
        private bool modified;
        private float voxel_size;
        private Vector3Int dimensions, c_dim;
        public List<Chunk> chunk_data;

        //READONLY
        public Vector3Int Dimensions
        {
            get { return dimensions; }
        }

        public bool Modified
        {
            get { return modified; }
        }

        //Constructor takes game object to instance to, voxel size, and Vector3Int dimensions in voxels
        public Chunk(GameObject g, float v, Vector3Int d, Vector3Int c_d, in List<Chunk> c_dat) 
        {
            chunk_GO = g;
            index = int.Parse(g.name.Split(' ')[1]);
            modified = false;
            voxel_size = v;

            //First initialize the terrain data to be a big cube of voxel data
            terrain_data = new BitArray[d.x, d.y];
            for (int x = 0; x <= terrain_data.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= terrain_data.GetUpperBound(1); y++)
                    terrain_data[x, y] = new BitArray(d.z, true);
            }

            dimensions = d;
            c_dim = c_d;
            chunk_data = c_dat;
            chunk_GO.GetComponent<MeshFilter>().mesh = new Mesh();
            chunk_GO.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GenerateMesh(true);
        }

        // Function will generate a mesh based off from voxel data
        public void GenerateMesh(bool initial_generation = false)
        {
            Mesh mesh = chunk_GO.GetComponent<MeshFilter>().mesh;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            Vector3 half_map = new Vector3(terrain_data.GetUpperBound(0) + 1, terrain_data.GetUpperBound(1) + 1, terrain_data[0, 0].Length);
            half_map /= 2.0f;

            int triangle_offset = 0;
            //generate a voxel for each spot that isn't an air pocket
            for (int x = 0; x <= terrain_data.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= terrain_data.GetUpperBound(1); y++)
                {
                    for (int z = 0; z < terrain_data[x, y].Length; z++)
                    {
                        //if this is an air pocket don't do anything
                        if (terrain_data[x, y].Get(z) == false)
                            continue;

                        //first calculate the position of this voxel
                        float x_offset = .5f;
                        float y_offset = .5f;
                        float z_offset = .5f;

                        Vector3 voxel_location = new Vector3((-half_map.x + x + x_offset), (-half_map.y + y + y_offset), (-half_map.z + z + z_offset));
                        voxel_location *= voxel_size;

                        //using that location, now calculate each of this voxel's vertex locations and store them
                        int[,] vertex_uoffsets = new int[8, 3] { {-1, -1, -1}, {1, -1, -1 }, {-1, 1, -1}, {1, 1, -1},
                                                             {-1, -1, 1},  {1, -1, 1},   {-1, 1, 1},  {1, 1, 1}};

                        for (int v = 0; v <= vertex_uoffsets.GetUpperBound(0); v++)
                        {
                            float v_x = vertex_uoffsets[v, 0] * .5f * voxel_size;
                            float v_y = vertex_uoffsets[v, 1] * .5f * voxel_size;
                            float v_z = vertex_uoffsets[v, 2] * .5f * voxel_size;
                            Vector3 vertex_location = new Vector3(voxel_location.x + v_x, voxel_location.y + v_y, voxel_location.z + v_z);
                            vertices.Add(vertex_location);
                        }

                        //now check adjacent spaces to see if triangles should be drawn for that face
                        bool[] has_face = new bool[6] { false, false, false, false, false, false };
                        int[] potential_triangles = new int[36] {
                                                             4, 6, 2, 2, 0, 4, // left
                                                             4, 0, 1, 1, 5, 4, // down
                                                             0, 2, 3, 3, 1, 0, // back
                                                             1, 3, 5, 3, 7, 5, // right
                                                             2, 6, 3, 6, 7, 3, // up
                                                             7, 4, 5, 7, 6, 4, // forward
                                                            };

                        int zy_area = c_dim.z * c_dim.y;
                        for (int f = -1; f <= 1; f += 2)
                        {
                            for (int axis = 0; axis < 3; axis++)
                            {
                                int plane_index = axis + (f == 1 ? 3 : 0);

                                //x axis
                                if (axis == 0)
                                {
                                    int check_plane = x + f;
                                    //first see if the axis is outside the bounds of the terrain space
                                    if (check_plane < 0 || check_plane > terrain_data.GetUpperBound(0))
                                    {
                                        //draw a face if this chunk doesn't have a neighboring chunk
                                        if((index < zy_area && f == -1) || (index >= (c_dim.z * c_dim.y * c_dim.x) - (zy_area) && f == 1))
                                            has_face[plane_index] = true;

                                        //if this is the first time generating, continue
                                        if (initial_generation)
                                            continue;
                                        //if not a face voxel continue
                                        if (x != 0 && x < dimensions.x - 1)
                                            continue;

                                        int left_chunk = index - c_dim.z * c_dim.y;
                                        int right_chunk = index + c_dim.z * c_dim.y;

                                        //has left chunk
                                        if(index >= c_dim.z * c_dim.y)
                                        {
                                            if (x == 0)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(dimensions.x - 1, y, z);
                                                if (chunk_data[left_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }
                                        //has right chunk
                                        if (index < chunk_data.Count - c_dim.z * c_dim.y)
                                        {
                                            if (x == dimensions.x - 1)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(0, y, z);
                                                if (chunk_data[right_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }

                                        continue;
                                    }

                                    //next see if their is an air pocket next to this voxel
                                    if (terrain_data[check_plane, y].Get(z) == false)
                                        has_face[plane_index] = true;

                                }
                                //y axis
                                else if (axis == 1)
                                {
                                    int check_plane = y + f;
                                    //first see if the axis is outside the bounds of the terrain space
                                    if (check_plane < 0 || check_plane > terrain_data.GetUpperBound(1))
                                    {
                                        //draw a face if this chunk doesn't have a neighboring chunk
                                        if((index % zy_area < c_dim.z && f == -1) || (index % zy_area >= (zy_area - c_dim.z) && f == 1))
                                            has_face[plane_index] = true;

                                        //if this is the first time generating, continue
                                        if (initial_generation)
                                            continue;

                                        //if not a face voxel continue
                                        if (y != 0 && y < dimensions.y - 1)
                                            continue;

                                        int below_chunk = index - c_dim.z;
                                        int above_chunk = index + c_dim.z;

                                        //has bottom chunk
                                        if (index % (c_dim.y * c_dim.z) >= c_dim.z)
                                        {
                                            if (y == 0)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(x, dimensions.y - 1, z);
                                                if (chunk_data[below_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }
                                        //has above chunk
                                        if (index % (c_dim.y * c_dim.z) < c_dim.y * c_dim.z - c_dim.z)
                                        {
                                            if (y == dimensions.y - 1)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(x, 0, z);
                                                if (chunk_data[above_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }

                                        continue;
                                    }

                                    //next see if their is an air pocket next to this voxel
                                    if (terrain_data[x, check_plane].Get(z) == false)
                                        has_face[plane_index] = true;
                                }
                                //z axis
                                else
                                {
                                    int check_plane = z + f;
                                    //first see if the axis is outside the bounds of the terrain space
                                    if (check_plane < 0 || check_plane >= terrain_data[x, y].Length)
                                    {
                                        if((index % c_dim.z == 0 && f == -1) || ((index + 1) % c_dim.z == 0 && f == 1))
                                            has_face[plane_index] = true;

                                        //if this is the first time generating, continue
                                        if (initial_generation)
                                            continue;

                                        //if not a face voxel continue
                                        if (z != 0 && z < dimensions.z - 1)
                                            continue;

                                        int back_chunk = index - 1;
                                        int forward_chunk = index + 1;

                                        //has back chunk
                                        if (index % c_dim.z > 0)
                                        {
                                            if (z == 0)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(x, y, dimensions.z - 1);
                                                if (chunk_data[back_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }
                                        //has forward chunk
                                        if (index % c_dim.z < c_dim.z - 1)
                                        {
                                            if (z == dimensions.z - 1)
                                            {
                                                //check if the neighboring voxel is air
                                                Vector3Int neighboring_voxel = new Vector3Int(x, y, 0);
                                                if (chunk_data[forward_chunk].terrain_data[neighboring_voxel.x, neighboring_voxel.y].Get(neighboring_voxel.z) == false)
                                                    has_face[plane_index] = true;
                                            }
                                        }

                                        continue;
                                    }
                                    //next see if their is an air pocket next to this voxel
                                    if (terrain_data[x, y].Get(check_plane) == false)
                                        has_face[plane_index] = true;
                                }
                            }
                        }

                        // store the triangle data
                        int tri_index = triangle_offset * 8;
                        for (int face = 0; face < has_face.Length; face++)
                        {
                            if (has_face[face])
                            {
                                for (int triangle = 0; triangle < 6; triangle++)
                                {
                                    triangles.Add(potential_triangles[triangle + (6 * face)] + tri_index);
                                }
                            }
                        }

                        triangle_offset++;

                        //finally calculate normal data
                        for(int j=0; j<=vertex_uoffsets.GetUpperBound(0); j++)
                        {
                            normals.Add(new Vector3(vertex_uoffsets[j, 0], vertex_uoffsets[j, 1], vertex_uoffsets[j, 2]));
                        }
                    }
                }
            }

            //apply vertices to the mesh
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.Optimize();
            mesh.name = $"Chunk {index} mesh";
            if(triangles.Count > 0)
                Physics.BakeMesh(mesh.GetInstanceID(), false);
        }

        //Function aids in converting worldspace coordinates to voxelspace for use in erode functions
        public Vector3Int WorldToVoxel(Vector3 position)
        {
            //get half the bottom back left corner of the chunk
            Vector3 bottom_back_left = -dimensions / 2;
            bottom_back_left.x -= dimensions.x % 2 == 0 ? 0 : .5f;
            bottom_back_left.y -= dimensions.y % 2 == 0 ? 0 : .5f;
            bottom_back_left.z -= dimensions.z % 2 == 0 ? 0 : .5f;
            bottom_back_left *= voxel_size;

            //first convert the position to localspace of this chunk then orient it towards the bottom left corner of the chunk
            //scale this local_position down by the voxel size and integer divide it to get the voxel location of this point
            Vector3 local_position = position - (bottom_back_left + chunk_GO.transform.position);
            local_position /= voxel_size;

            Vector3Int voxel_space = new Vector3Int((int)local_position.x, (int)local_position.y, (int)local_position.z);
            voxel_space.x = Mathf.Clamp(voxel_space.x, 0, dimensions.x - 1);
            voxel_space.y = Mathf.Clamp(voxel_space.y, 0, dimensions.y - 1);
            voxel_space.z = Mathf.Clamp(voxel_space.z, 0, dimensions.z - 1);

            return voxel_space;
        }

        //Function generates a voxel sphere for the purposes of mass erosion
        public List<Vector3Int> GenVoxelSphere(Vector3Int starting_location, int radius)
        {
            if(radius < 1)
            {
                Debug.LogWarning("Provided voxel sphere radius is smaller than possible, clamping...");
                radius = 1;
            }

            List<Vector3Int> output_list = new List<Vector3Int>();
            List<Vector3Int> relative_output_list = new List<Vector3Int>();
            //generate the relative positive quadrant of the sphere
            for (int x=0; x<radius; x++)
            {
                for(int y=0; y<radius; y++)
                {
                    for(int z=0; z<radius; z++)
                    {
                        //check if the distance of this voxel is too far from the center
                        Vector3Int voxel_location = starting_location + new Vector3Int(x, y, z);
                        int distance = (int)Vector3Int.Distance(starting_location, voxel_location);
                        if (distance > radius)
                            continue;
                        relative_output_list.Add(new Vector3Int(x, y, z));
                    }
                }
            }


            //generate the 8 quadrants of the sphere based off from the relative generated quadrant
            for(int i = 0; i<relative_output_list.Count; i++)
            {
                // + + + 
                output_list.Add(starting_location + new Vector3Int(relative_output_list[i].x, relative_output_list[i].y, relative_output_list[i].z));
                // - + +
                output_list.Add(starting_location + new Vector3Int(-relative_output_list[i].x, relative_output_list[i].y, relative_output_list[i].z));
                // - - +
                output_list.Add(starting_location + new Vector3Int(-relative_output_list[i].x, -relative_output_list[i].y, relative_output_list[i].z));
                // - - -
                output_list.Add(starting_location + new Vector3Int(-relative_output_list[i].x, -relative_output_list[i].y, -relative_output_list[i].z));
                // + - +
                output_list.Add(starting_location + new Vector3Int(relative_output_list[i].x, -relative_output_list[i].y, relative_output_list[i].z));
                // + - -
                output_list.Add(starting_location + new Vector3Int(relative_output_list[i].x, -relative_output_list[i].y, -relative_output_list[i].z));
                // - + -
                output_list.Add(starting_location + new Vector3Int(-relative_output_list[i].x, relative_output_list[i].y, -relative_output_list[i].z));
                // + + -
                output_list.Add(starting_location + new Vector3Int(relative_output_list[i].x, relative_output_list[i].y, -relative_output_list[i].z));
            }

            return output_list;
        }

        // Helper function that updates neighboring chunks to a location
        public void UpdateNeighboringChunk(Vector3Int voxel_location)
        {
            if (voxel_location.x < 0 || voxel_location.x >= dimensions.x)
            {
                Debug.LogWarning("Tried to update neighbors with voxel at invalid x location!");
                return;
            }
            if (voxel_location.y < 0 || voxel_location.y >= dimensions.y)
            {
                Debug.LogWarning("Tried to update neighbors with voxel at invalid y location!");
                return;
            }
            if (voxel_location.z < 0 || voxel_location.z >= dimensions.z)
            {
                Debug.LogWarning("Tried to update neighbors with voxel at invalid z location!");
                return;
            }

            //if this is a border voxel location, update the neighboring chunk
            //left
            if (voxel_location.x == 0)
            {
                int border_c_index = index - c_dim.z * c_dim.y;
                if (border_c_index >= 0)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if(border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
            //right
            if (voxel_location.x == dimensions.x - 1)
            {
                int border_c_index = index + c_dim.z * c_dim.y;
                if (border_c_index < chunk_data.Count)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if (border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
            //below
            if (voxel_location.y == 0)
            {
                int border_c_index = index - c_dim.z;
                if (border_c_index >= 0)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if (border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
            //above
            if (voxel_location.y == dimensions.y - 1)
            {
                int border_c_index = index + c_dim.z;
                if (border_c_index < chunk_data.Count)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if (border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
            //backwards
            if (voxel_location.z == 0)
            {
                int border_c_index = index - 1;
                if (border_c_index >= 0)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if (border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
            //forwards
            if (voxel_location.z == dimensions.z - 1)
            {
                int border_c_index = index + 1;
                if (border_c_index < chunk_data.Count)
                {
                    Chunk border_chunk = chunk_data[border_c_index];
                    border_chunk.GenerateMesh();
                    if (border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().sharedMesh = border_chunk.chunk_GO.GetComponent<MeshFilter>().mesh;
                    else
                        border_chunk.chunk_GO.GetComponent<MeshCollider>().enabled = false;
                }
            }
        }

        // Function will allow erosion of a voxel given the voxel location
        public void Erode(Vector3Int voxel_location)
        {
            if (voxel_location.x < 0 || voxel_location.x >= dimensions.x)
            {
                Debug.LogWarning("Tried to erode voxel at invalid x location!");
                return;
            }
            if (voxel_location.y < 0 || voxel_location.y >= dimensions.y)
            {
                Debug.LogWarning("Tried to erode voxel at invalid y location!");
                return;
            }
            if (voxel_location.z < 0 || voxel_location.z >= dimensions.z)
            {
                Debug.LogWarning("Tried to erode voxel at invalid z location!");
                return;
            }

            modified = true;
            terrain_data[voxel_location.x, voxel_location.y].Set(voxel_location.z, false);
            GenerateMesh();
            if (chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                chunk_GO.GetComponent<MeshCollider>().sharedMesh = chunk_GO.GetComponent<MeshFilter>().mesh;
            else
                chunk_GO.GetComponent<MeshCollider>().enabled = false;
            UpdateNeighboringChunk(voxel_location);
        }

        // Function will erode a series of voxels given a list of their locations
        public void Erode(List<Vector3Int> voxel_locations)
        {
            List<Vector3Int> update_voxels = new List<Vector3Int>();
            //left right down up back front
            bool[] flagged_directions = new bool[6] { false, false, false, false, false, false };
            for (int i = 0; i < voxel_locations.Count; i++)
            {
                bool skip_erode = false;
                if (voxel_locations[i].x < 0 || voxel_locations[i].x >= dimensions.x)
                {
                    //Debug.LogWarning("Tried to erode voxel at invalid x location!");
                    skip_erode = true;
                }
                if (voxel_locations[i].y < 0 || voxel_locations[i].y >= dimensions.y)
                {
                    //Debug.LogWarning("Tried to erode voxel at invalid y location!");
                    skip_erode = true;
                }
                if (voxel_locations[i].z < 0 || voxel_locations[i].z >= dimensions.z)
                {
                    //Debug.LogWarning("Tried to erode voxel at invalid z location!");
                    skip_erode = true;
                }

                if (skip_erode)
                    continue;

                modified = true;
                terrain_data[voxel_locations[i].x, voxel_locations[i].y].Set(voxel_locations[i].z, false);

                //mark border chunks for updating
                //left
                if(voxel_locations[i].x == 0)
                    flagged_directions[0] = true;
                //right
                if (voxel_locations[i].x == dimensions.x - 1)
                    flagged_directions[1] = true;
                //down
                if (voxel_locations[i].y == 0)
                    flagged_directions[2] = true;
                //up
                if (voxel_locations[i].y == dimensions.y - 1)
                    flagged_directions[3] = true;
                //back
                if (voxel_locations[i].z == 0)
                    flagged_directions[4] = true;
                //forward
                if (voxel_locations[i].z == dimensions.z - 1)
                    flagged_directions[5] = true;
            }
            GenerateMesh();
            if (chunk_GO.GetComponent<MeshFilter>().mesh.vertexCount > 0)
                chunk_GO.GetComponent<MeshCollider>().sharedMesh = chunk_GO.GetComponent<MeshFilter>().mesh;
            else
                chunk_GO.GetComponent<MeshCollider>().enabled = false;

            for(int i=0; i<6; i++)
            {
                if(flagged_directions[i] == true)
                {
                    switch (i)
                    {
                        case 0:
                            UpdateNeighboringChunk(new Vector3Int(0, 1, 1));
                            break;
                        case 1:
                            UpdateNeighboringChunk(new Vector3Int(dimensions.x - 1, 1, 1));
                            break;
                        case 2:
                            UpdateNeighboringChunk(new Vector3Int(1, 0, 1));
                            break;
                        case 3:
                            UpdateNeighboringChunk(new Vector3Int(1, dimensions.y - 1, 1));
                            break;
                        case 4:
                            UpdateNeighboringChunk(new Vector3Int(1, 1, 0));
                            break;
                        case 5:
                            UpdateNeighboringChunk(new Vector3Int(1, 1, dimensions.z - 1));
                            break;
                    }
                }
            }
        }
    }

    //function generates a series of chunks and organizes them into the chunks list
    void GenerateChunks()
    {
        chunks = new List<Chunk>();
        int chunk_index = 0;
        Vector3Int half_dim = chunk_dim / 2;
        for(int x = 0; x < chunk_dim.x; x++)
        {
            for(int y = 0; y < chunk_dim.y; y++)
            {
                for(int z = 0; z < chunk_dim.z; z++)
                {
                    //Instance a chunk object
                    GameObject chunk_object = new GameObject($"Chunk {chunk_index}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                    chunk_object.layer = LayerMask.NameToLayer("Terrain");

                    //Make the chunk a child of this object and move it to it's proper location
                    chunk_object.transform.parent = transform;
                    float matrix_x = -half_dim.x + x + ((chunk_dim.x % 2) == 0 ? 0.5f : 0.0f);
                    float matrix_y = -half_dim.y + y + ((chunk_dim.y % 2) == 0 ? 0.5f : 0.0f);
                    float matrix_z = -half_dim.z + z + ((chunk_dim.z % 2) == 0 ? 0.5f : 0.0f);

                    Vector3 chunk_position = new Vector3(matrix_x * (chunk_voxel_dim.x), matrix_y * (chunk_voxel_dim.y), matrix_z * chunk_voxel_dim.z);
                    chunk_position *= voxel_size;
                    chunk_object.transform.position = chunk_position;

                    //Generate the chunk's geometry
                    chunk_object.GetComponent<MeshRenderer>().material = terrain_mat;
                    Chunk test_chunk = new Chunk(chunk_object, voxel_size, chunk_voxel_dim, chunk_dim, in chunks);
                    if (chunk_object.GetComponent<MeshFilter>().mesh.triangles.Length > 0)
                        chunk_object.GetComponent<MeshCollider>().sharedMesh = chunk_object.GetComponent<MeshFilter>().mesh;

                    //Add chunk to list of chunks
                    chunks.Add(test_chunk);
                    chunk_index++;
                }
            }
        }

        Debug.Log($"Finished loading: {chunks[0].chunk_data.Count} chunks");
    }

    public void UpdateMaterials()
    {
        if (GameManager.instance == null)
            return;

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(0).GetComponent<MeshRenderer>().material = GameManager.instance.GetGroundMaterial();
    }

    IEnumerator GenerateChunksSlow()
    {
        yield return null;
        chunks = new List<Chunk>();
        int chunk_index = 0;
        Vector3Int half_dim = chunk_dim / 2;
        for (int x = 0; x < chunk_dim.x; x++)
        {
            for (int y = 0; y < chunk_dim.y; y++)
            {
                for (int z = 0; z < chunk_dim.z; z++)
                {
                    //Instance a chunk object
                    GameObject chunk_object = new GameObject($"Chunk {chunk_index}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                    chunk_object.layer = LayerMask.NameToLayer("Terrain");

                    //Make the chunk a child of this object and move it to it's proper location
                    chunk_object.transform.parent = transform;
                    float matrix_x = -half_dim.x + x + ((chunk_dim.x % 2) == 0 ? 0.5f : 0.0f);
                    float matrix_y = -half_dim.y + y + ((chunk_dim.y % 2) == 0 ? 0.5f : 0.0f);
                    float matrix_z = -half_dim.z + z + ((chunk_dim.z % 2) == 0 ? 0.5f : 0.0f);

                    Vector3 chunk_position = new Vector3(matrix_x * (chunk_voxel_dim.x), matrix_y * (chunk_voxel_dim.y), matrix_z * chunk_voxel_dim.z);
                    chunk_position *= voxel_size;
                    chunk_object.transform.position = chunk_position;

                    //Generate the chunk's geometry
                    chunk_object.GetComponent<MeshRenderer>().material = terrain_mat;
                    Chunk test_chunk = new Chunk(chunk_object, voxel_size, chunk_voxel_dim, chunk_dim, in chunks);
                    if (chunk_object.GetComponent<MeshFilter>().mesh.triangles.Length > 0)
                        chunk_object.GetComponent<MeshCollider>().sharedMesh = chunk_object.GetComponent<MeshFilter>().mesh;

                    //hide the chunk for additive loading
                    chunk_object.GetComponent<MeshRenderer>().enabled = false;
                    chunk_object.GetComponent<MeshCollider>().enabled = false;

                    //Add chunk to list of chunks
                    chunks.Add(test_chunk);
                    chunk_index++;

                    if(!force_load)
                        yield return new WaitForEndOfFrame();
                }
            }
        }

        Debug.Log($"Finished loading: {chunks[0].chunk_data.Count} chunks");
    }

    IEnumerator GenerateSlow()
    {
        finished_generating = false;
        StartCoroutine(GenerateChunksSlow());
        yield return new WaitUntil(() => transform.childCount == chunk_dim.x * chunk_dim.y * chunk_dim.z);
        finished_generating = true;
    }

    //function will generate a cave cross section for enemy spawns
    void GenerateCave(Vector3 position, int radius, int cave_size, bool spawn_enemy = false)
    {
        //some precautions
        if(cave_size < 0)
        {
            Debug.LogWarning($"Cannot erode a cave with size: {cave_size}, aborting...");
            return;
        }

        //start by getting the seed chunk to erode at, erode a starting sphere there
        Vector3Int chunk_position = ConvertWLocationtoChunkLocation(position);
        int chunk_index = GetChunkIndexAtLocation(chunk_position);
        ErodeChunkSphereAtLocation(chunk_index, radius, position);

        //erode 4 branches coming off from this point equal to the cave size
        for(int direction = 0; direction < 4; direction++)
        {
            for(int i = 0; i<cave_size; i++)
            {
                //calculate the vector to erode at
                Vector3 erosion_vector = new Vector3(0.0f, 0.0f, 1.0f);
                switch (direction)
                {
                    case 1:
                        erosion_vector = new Vector3(1.0f, 0.0f, 0.0f);
                        break;
                    case 2:
                        erosion_vector = new Vector3(0.0f, 0.0f, -1.0f);
                        break;
                    case 3:
                        erosion_vector = new Vector3(-1.0f, 0.0f, 0.0f);
                        break;
                    default:
                        break;
                }

                //fire a ray in the direction of the erosion vector
                RaycastHit hit;
                LayerMask ignore_world_bounds = LayerMask.GetMask("Terrain");
                if (Physics.Raycast(position, erosion_vector, out hit, (i + 1) * radius / 2.0f, ignore_world_bounds))
                {
                    //Debug.DrawRay(position, erosion_vector * (i + 1) * radius / 2.0f, Color.blue, Mathf.Infinity);

                    //erode the chunk that this ray hits
                    int erosion_index = int.Parse(hit.collider.gameObject.name.Split(' ')[1]);
                    ErodeChunkSphereAtLocation(erosion_index, radius, hit.point - hit.normal * cave_erosion_error);
                    nav_tree.AddNode(hit.point - hit.normal * cave_erosion_error);
                }
            }
        }

        //optionally spawn an enemy
        if (spawn_enemy)
        {
            int enemy_index = Random.Range(0, enemies.Length);
            if(enemies.Length != 0)
                Instantiate(enemies[enemy_index], position, Quaternion.identity);
        }
    }

    //function erodes caves after initial generation
    public void GenerateCaves(bool generate_shaft = true)
    {
        //sanity check to make sure we know our nav tree
        if(nav_tree == null)
        {
            GameObject[] nav_trees = GameObject.FindGameObjectsWithTag("NavTree");
            for (int i = 0; i < nav_trees.Length; i++)
            {
                if (nav_trees[i].scene == gameObject.scene)
                    nav_tree = GameObject.FindGameObjectWithTag("NavTree").GetComponent<NavTree>();
            }
        }

        //generate the initial tunnel at the center of the map by first generating a cave, and then generating a shaft up the center
        GenerateCave(new Vector3(0, 0, 0), erode_radius, 2);
        if (generate_shaft)
        {
            bool generating_shaft = true;
            int shaft_iterations = 0;
            while (generating_shaft)
            {
                generating_shaft = false;
                RaycastHit shaft_cast;
                Debug.DrawRay(Vector3.zero, Vector3.up * (shaft_iterations + 1) * erode_radius, Color.red, Mathf.Infinity);
                LayerMask ignore_player_pp_wb = LayerMask.GetMask("Terrain");
                if (Physics.Raycast(Vector3.zero, Vector3.up, out shaft_cast, (shaft_iterations + 1) * erode_radius, ignore_player_pp_wb))
                {
                    generating_shaft = true;
                    int erosion_index = int.Parse(shaft_cast.collider.gameObject.name.Split(' ')[1]);
                    ErodeChunkSphereAtLocation(erosion_index, erode_radius, shaft_cast.point - shaft_cast.normal * cave_erosion_error);
                    nav_tree.AddNode(shaft_cast.point - shaft_cast.normal * cave_erosion_error);
                }
                shaft_iterations++;
            }
        }

        //pick a random number of caves to generate and generate them (they cannot be close to the center chunks at all)
        //the cave may also not be close to the surface
        int num_caves = Random.Range(4, 8);
        
        for(int i = 0; i< num_caves; i++)
        {
            bool valid_position = false;
            int chunk_index = 0;
            while (!valid_position)
            {
                valid_position = true;
                chunk_index = Random.Range(0, chunks.Count);
                Vector3 chunk_xz = new Vector3(chunks[chunk_index].chunk_GO.transform.position.x, 0.0f, chunks[chunk_index].chunk_GO.transform.position.z);
                if (chunk_xz.magnitude < 10.0f)
                    valid_position = false;

                if (chunks[chunk_index].chunk_GO.transform.position.y > (GetMapDimensions().y / 2) * .6f)
                    valid_position = false;
            }
            GenerateCave(chunks[chunk_index].chunk_GO.transform.position, erode_radius, Random.Range(2, 4), true);
        }

        GenerateRocks();
    }

    //function will erode a spot and then spawn a rock at teh given chunk
    public void GenerateRock(int index)
    {
        ErodeChunkSphereAtLocation(index, 6, chunks[index].chunk_GO.transform.position);
        Instantiate(rock, chunks[index].chunk_GO.transform.position - rock.GetComponent<BoxCollider>().center, Quaternion.identity);
    }

    //function will attempt to generate a random amount of rocks, provided that those chunks have not been modified
    public void GenerateRocks()
    {
        int num_rocks = Random.Range(8, 16);

        for(int i=0; i<num_rocks; i++)
        {
            bool valid_position = false;
            int chunk_index = 0;
            while (!valid_position)
            {
                valid_position = true;
                chunk_index = Random.Range(0, chunks.Count);

                //the chunk cannot be modified, or too close to the surface
                if (chunks[chunk_index].Modified)
                    valid_position = false;

                if (chunks[chunk_index].chunk_GO.transform.position.y > (GetMapDimensions().y / 2) * .6f)
                    valid_position = false;
            }
            GenerateRock(chunk_index);
        }
    }

    //function gets the map size to help with nav generation logic
    public Vector3 GetMapDimensions()
    {
        Vector3 map_dimensions = new Vector3(chunk_dim.x * chunk_voxel_dim.x, chunk_dim.y * chunk_voxel_dim.y, chunk_dim.z * chunk_voxel_dim.z);
        map_dimensions *= voxel_size;
        return map_dimensions;
    }

    //function gets the chunk index given its x y z position in the terrain data
    public int GetChunkIndexAtLocation(Vector3Int chunk_position)
    {
        if (chunk_position.x < 0 || chunk_position.y < 0 || chunk_position.z < 0)
            return -1;
        if (chunk_position.x >= chunk_dim.x || chunk_position.y >= chunk_dim.y || chunk_position.z >= chunk_dim.z)
            return -1;
        return chunk_dim.z * chunk_dim.y * chunk_position.x + chunk_dim.z * chunk_position.y + chunk_position.z;
    }

    //function get the chunk index given its x y z position in the world
    public Vector3Int ConvertWLocationtoChunkLocation(Vector3 position)
    {
        Vector3 map_dimensions = new Vector3(chunk_dim.x * chunk_voxel_dim.x, chunk_dim.y * chunk_voxel_dim.y, chunk_dim.z * chunk_voxel_dim.z);
        map_dimensions *= voxel_size;
        //first off, get the back bottom left corner of the map
        Vector3 back_bottom_left = new Vector3(-map_dimensions.x / 2.0f, -map_dimensions.y / 2.0f, -map_dimensions.z / 2.0f);
        back_bottom_left.x -= back_bottom_left.x % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.y -= back_bottom_left.y % 2 == 0 ? 0 : 0.5f;
        back_bottom_left.z -= back_bottom_left.z % 2 == 0 ? 0 : 0.5f;

        //now get the vector this position makes with the bottom left corner and scale it down by the dimensions of the map
        Vector3 chunk_space_vector = position - back_bottom_left;
        chunk_space_vector.x /= chunk_voxel_dim.x;
        chunk_space_vector.y /= chunk_voxel_dim.y;
        chunk_space_vector.z /= chunk_voxel_dim.z;
        chunk_space_vector /= voxel_size;

        //return this casted as an integer
        return new Vector3Int((int)chunk_space_vector.x, (int)chunk_space_vector.y, (int)chunk_space_vector.z);
    }

    //Function gets the chunk x y z position in the terrain data given its index
    public Vector3Int GetChunkLocationAtIndex(int chunk_index)
    {
        int x_location = chunk_index / (chunk_dim.z * chunk_dim.y);
        int y_location = chunk_index / chunk_dim.z - x_location * chunk_dim.z;
        int z_location = chunk_index % chunk_dim.z;
        return new Vector3Int(x_location, y_location, z_location);
    }

    public void ErodeChunkPointAtLocation(int chunk_index, Vector3 erode_location)
    {
        chunks[chunk_index].Erode(chunks[chunk_index].WorldToVoxel(erode_location));
    }
    public void ErodeChunkSphereAtLocation(int chunk_index, int radius, Vector3 erode_location)
    {
        Vector3Int erode_point = chunks[chunk_index].WorldToVoxel(erode_location);
        List<Vector3Int> sphere = chunks[chunk_index].GenVoxelSphere(erode_point, radius);

        //if there are any voxels that go outside of the bounds of a chunk, then try to erode an adjacent chunk

        chunks[chunk_index].Erode(sphere);

        //first loop through all sphere locations and try to recognize what neighboring chunk needs to be eroded
        Vector3Int chunk_location = GetChunkLocationAtIndex(chunk_index);
        Dictionary<int, List<Vector3Int>> erosion_dictionary = new Dictionary<int, List<Vector3Int>>();
        for(int i=0; i<sphere.Count; i++)
        {
            //determine if a voxel is invalid
            if(sphere[i].x < 0 || sphere[i].x >= chunk_voxel_dim.x ||
                sphere[i].y < 0 || sphere[i].y >= chunk_voxel_dim.y ||
                sphere[i].z < 0 || sphere[i].z >= chunk_voxel_dim.z)
            {
                //calculate the neighboring chunk's index
                int x_offset = 0, y_offset = 0, z_offset = 0;

                if (sphere[i].x < 0 || sphere[i].x >= chunk_voxel_dim.x)
                    x_offset = sphere[i].x < 0 ? -1 : 1;

                if (sphere[i].y < 0 || sphere[i].y >= chunk_voxel_dim.y)
                    y_offset = sphere[i].y < 0 ? -1 : 1;

                if (sphere[i].z < 0 || sphere[i].z >= chunk_voxel_dim.z)
                    z_offset = sphere[i].z < 0 ? -1 : 1;

                int neighboring_index = GetChunkIndexAtLocation(new Vector3Int(x_offset, y_offset, z_offset) + chunk_location);

                //skip if this chunk doesn't have a neighboring chunk
                if (neighboring_index < 0 || neighboring_index >= chunks.Count)
                    continue;

                //calculate the new xyz position of the voxel to erode
                Vector3Int new_erode = sphere[i];
                //left
                if (x_offset == -1)
                    new_erode.x = sphere[i].x + chunk_voxel_dim.x;
                //right
                else if (x_offset == 1)
                    new_erode.x = sphere[i].x - chunk_voxel_dim.x;
                //down
                if (y_offset == -1)
                    new_erode.y = sphere[i].y + chunk_voxel_dim.y;
                //up
                else if (y_offset == 1)
                    new_erode.y = sphere[i].y - chunk_voxel_dim.y;
                //back
                if (z_offset == -1)
                    new_erode.z = sphere[i].z + chunk_voxel_dim.z;
                //forward
                else if (z_offset == 1)
                    new_erode.z = sphere[i].z - chunk_voxel_dim.z;

                //check to see if the neighboring chunk already has voxels to erode, if it does, then add the new voxel to it, if it doesn't then initialize a new list
                //and add the voxel
                if (erosion_dictionary.ContainsKey(neighboring_index))
                    erosion_dictionary[neighboring_index].Add(new_erode);
                else
                {
                    erosion_dictionary.Add(neighboring_index, new List<Vector3Int>());
                    erosion_dictionary[neighboring_index].Add(new_erode);
                }
            }
        }

        //finally erode every voxel from adjacent chunks
        foreach (KeyValuePair<int, List<Vector3Int>> i in erosion_dictionary)
        {
            chunks[i.Key].Erode(i.Value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] nav_trees = GameObject.FindGameObjectsWithTag("NavTree");
        for (int i = 0; i < nav_trees.Length; i++)
        {
            if(nav_trees[i].scene == gameObject.scene)
                nav_tree = GameObject.FindGameObjectWithTag("NavTree").GetComponent<NavTree>();
        }
        //if statement is to make it easier to test precached terrain generations
        if (!slow)
        {
            if (transform.childCount == 0)
                GenerateChunks();

            //Generate a caves to spawn enemies in
            GenerateCaves(false);
        }
        else
        {
            if (transform.childCount == 0)
                StartCoroutine(GenerateSlow());
        }

    }
}
