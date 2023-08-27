using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEnemyAI : MonoBehaviour
{
    public float ghost_volume;
    public float move_speed;
    [SerializeField]
    protected int boredom_counter;
    protected int boredom_max, pump_level;
    [SerializeField]
    protected float player_pathfind_timer, wander_timer, pump_timer;
    protected float player_pathfind_timer_max, wander_timer_max, pump_timer_max;
    [SerializeField]
    protected bool can_path_to_player, wandering, ghost, in_wall, already_checked_exit, can_reach_exit, reached_exit, is_being_pumped, dead;
    protected bool squished;
    [SerializeField]
    protected bool[] last_enemy_flags;                            //flags for the last enemy's logic 0=is the last enemy, 1=reached the center of the map, 2=reached the surface, 3 = reached the edge of the map
    protected Vector3 starting_position, ghost_destination;
    protected NavTree nav_tree;
    protected TerrainGeneration generator;
    protected List<Vector3> path_queue;
    protected Rigidbody rb;
    protected AudioSource ghost_sound, pop_sound;

    public virtual IEnumerator DeathSequence()
    {
        pop_sound.Play();
        dead = true;
        //calculate points based on how high up the enemy is
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            PlayerMovement player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
            if (transform.position.y > player.GetLayerOffsets()[1])
                GameManager.instance.score += 200;
            else if (transform.position.y > player.GetLayerOffsets()[2])
                GameManager.instance.score += 300;
            else if (transform.position.y > player.GetLayerOffsets()[3])
                GameManager.instance.score += 400;
            else
                GameManager.instance.score += 500;
        }
        yield return new WaitForSeconds(.4f);
        GameManager.instance.alive_enemies--;
        Destroy(gameObject);
    }

    public bool GetGhost() { return ghost; }

    public bool GetDead() { return dead; }

    public bool GetSquished() { return squished; }

    public void SetSquished(bool s) { squished = s; }

    public int GetPumpLevel() { return pump_level; }

    public void ResetAI() { 
        rb.position = starting_position;

        can_path_to_player = false;
        wandering = false;
        ghost = false;
        in_wall = false;
        already_checked_exit = false;
        can_reach_exit = false;
        reached_exit = false;

        last_enemy_flags = new bool[4] { false, false, false, false };

        boredom_max = boredom_counter;
        boredom_counter = boredom_max;

        player_pathfind_timer = player_pathfind_timer_max;

        wander_timer = wander_timer_max;

        pump_timer = 0.0f;
        pump_level = 0;

        ghost_sound.volume = 0.0f;
    }

    public void Escape() { last_enemy_flags[0] = true; }
    public bool Escaped() { return last_enemy_flags[3]; }

    class EasierNavNode
    {
        public float distance;
        public EasierNavNode prev_node;
        public NavTree.NavNode node;
    }

    //Driver function to the pathfinding algorithm
    bool Pathfind(Vector3 start, Vector3 end, bool only_check = false)
    {
        //first get the closest node to the starting position and ending position
        int starting_index = -1, ending_index = -1;
        NavTree.NavNode starting_node = nav_tree.GetClosestNodeToPoint(start, ref starting_index);
        NavTree.NavNode ending_node = nav_tree.GetClosestNodeToPoint(end, ref ending_index);

        //if we aren't close enough to a nav node, then don't do anything
        if (starting_index == -1 || ending_index == -1)
            return false;

        //then see if these nodes are connected at all, if they are then begin pathfinding, if not then return false
        List<NavTree.NavNode> connected_nodes = new List<NavTree.NavNode>();
        connected_nodes = nav_tree.GetChunk(start).GetAllConnectedNodes(starting_index);

        bool is_connected = false;
        for(int i=0; i<connected_nodes.Count; i++)
        {
            if(connected_nodes[i] == ending_node)
            {
                is_connected = true;
                break;
            }
        }

        if (!is_connected)
            return false;

        if (!only_check)
        {
            //prepare the necessary variables to start pathfinding
            EasierNavNode e_start = new EasierNavNode();
            e_start.node = starting_node;
            e_start.prev_node = null;
            e_start.distance = 0;

            EasierNavNode e_end = new EasierNavNode();
            e_end.node = ending_node;
            e_end.prev_node = null;
            e_end.distance = 0;

            List<EasierNavNode> visited = new List<EasierNavNode>();
            List<EasierNavNode> priority_queue = new List<EasierNavNode>();

            List<Vector3> path = Pathfind(e_start, e_end, ref visited, ref priority_queue);
            path.Reverse();
            path_queue = path;
        }

        return true;
    }

    //Function uses an implementation of the A* algorithm to try to find the quickest path to the endpoint
    List<Vector3> Pathfind(EasierNavNode node, EasierNavNode end, ref List<EasierNavNode> visited, ref List<EasierNavNode> priority_queue) {
        //get the node to start pathfinding with and remove it from the priority queue and add it to the visited queue
        for (int i = 0; i < priority_queue.Count; i++) {
            if (priority_queue[i] == node) 
            {
                priority_queue.RemoveAt(i);
            }
        }

        //loop through the neighbors of this node, if they are the goal then stop what you're doing and start constructing the return value
        //if they aren't then add them to the prioity queue if they already aren't in it and haven't already been visited
        //if they are in it, check if their GCost is less than what was previously known, if so then update it and set the previous node as this one
        foreach ((NavTree.NavNode, float) neighbor in node.node.connections) {
            if(neighbor.Item1 == end.node)
            {
                end.prev_node = node;
                List<Vector3> path = new List<Vector3>();
                CreatePath(end, ref path);
                return path;
            }
            bool found = false;
            int neighbor_index = -1;
            for(int j=0; j<priority_queue.Count; j++)
            {
                if(priority_queue[j].node == neighbor.Item1)
                {
                    found = true;
                    neighbor_index = j;
                    break;
                }
            }
            bool in_visited = false;
            for(int i = 0; i<visited.Count; i++)
            {
                if(visited[i].node == neighbor.Item1)
                {
                    in_visited = true;
                    break;
                }
            }
            if (!in_visited)
            {
                if (!found)
                {
                    EasierNavNode neighbor_node = new EasierNavNode();
                    neighbor_node.distance = neighbor.Item2 + node.distance;
                    neighbor_node.node = neighbor.Item1;
                    neighbor_node.prev_node = node;
                    priority_queue.Add(neighbor_node);
                }
                else
                {
                    if (priority_queue[neighbor_index].distance > neighbor.Item2 + node.distance)
                    {
                        priority_queue[neighbor_index].distance = neighbor.Item2 + node.distance;
                        priority_queue[neighbor_index].node = neighbor.Item1;
                        priority_queue[neighbor_index].prev_node = node;
                    }
                }
            }
        }

        //find the node in the priority queue with the lowest f cost (distance from the starting node + speculated distance from the ending node)
        float lowest_f_cost = Mathf.Infinity;
        int lowest_f_cost_index = int.MaxValue;
        for(int i=0; i<priority_queue.Count; i++)
        {
            float g_cost = priority_queue[i].distance;
            float h_cost = Vector3.Distance(priority_queue[i].node.position, end.node.position);
            
            if(g_cost + h_cost < lowest_f_cost)
            {
                lowest_f_cost_index = i;
                lowest_f_cost = g_cost + h_cost;
            }
        }

        visited.Add(node);
        return Pathfind(priority_queue[lowest_f_cost_index], end, ref visited, ref priority_queue);
    }

    //Helper function to construct a path at the end of the A* algorithm
    void CreatePath(EasierNavNode end_node, ref List<Vector3> path) 
    {
        if (end_node.prev_node == null)
            return;

        path.Add(end_node.node.position);
        CreatePath(end_node.prev_node, ref path);
    }
    
    //Function used for following nodes in the path queue for movement
    void FollowPath()
    {
        //abort if there are no nodes to pathfind to
        if (path_queue.Count == 0)
            return;

        //get the target node, if it is far away, move towards it, if not, remove it from the path queue
        Vector3 target_node = path_queue[0];

        if (Vector3.Distance(target_node, transform.position) >= move_speed * Time.deltaTime * 5.0f)
        {
            rb.velocity = (target_node - transform.position).normalized * move_speed;
            return;
        }

        path_queue.RemoveAt(0);
        rb.velocity = Vector3.zero;
    }

    //AI state that handles what happens when the enemy tries to escape
    void EscapeLogic()
    {
        //While the enemy hasn't reached the exit cave yet
        if(last_enemy_flags[1] == false)
        {//if the enemy is close enough to the exit, mark this flag as done
            if(Vector3.Distance(transform.position, Vector3.zero) < move_speed * Time.deltaTime)
            {
                last_enemy_flags[1] = true;
                return;
            }

            //if the enemy currently isn't ghosting and hasn't already checked try to see if it can pathfind there normally
            if (!ghost && !already_checked_exit)
            {
                already_checked_exit = true;
                can_reach_exit = Pathfind(transform.position, Vector3.zero);

                if (can_reach_exit)
                    Pathfind(transform.position, Vector3.zero);
            }

            //pathfind to the exit if the enemy can reach it, if the enemy cannot reach it, begin ghosting
            if (can_reach_exit)
            {
                FollowPath();

                //Sanity Check to move to the center of the map
                if (path_queue.Count == 0)
                    rb.velocity = -transform.position.normalized * move_speed;
                return;
            }
            else
            {
                ghost = true;
                SetGhostDestination();
                in_wall = false;
                GhostLogic(Vector3.zero);
            }
            return;
        }

        //While the enemy hasn't reached the surface yet move up until we've reached fresh air
        if(last_enemy_flags[2] == false)
        {
            if (ghost == true)
            {
                ghost_sound.volume = 0.0f;
                ghost = false;
            }
            float y_pos = (generator.GetMapDimensions().y / 2.0f) + GetComponent<Collider>().bounds.extents.y;

            if (transform.position.y < y_pos)
            {
                rb.velocity = Vector3.up * move_speed;
                return;
            }

            //if the enemy has reached the surface then mark this step as complete
            last_enemy_flags[2] = true;
            return;
        }

        //Try to move off the map
        Vector3 direction = new Vector3(transform.position.x, 0.0f, transform.position.z).normalized;
        if (direction == Vector3.zero)
            direction = Vector3.one;

        rb.velocity = direction * move_speed;

        //mark the last flag as complete when you've moved off the map
        float x_pos = (generator.GetMapDimensions().x / 2.0f);
        float z_pos = (generator.GetMapDimensions().z / 2.0f);

        if(Mathf.Abs(transform.position.x) > x_pos || Mathf.Abs(transform.position.z) > z_pos)
        {
            last_enemy_flags[3] = true;
            rb.velocity = Vector3.zero;
        }
    }

    //AI state that handles what happens when the enemy tries to chase the player
    void ChaseLogic()
    {
        //simply follow the precalculated path towards the player, if there isn't anything to follow then restart the pathfind timer
        if (path_queue.Count == 0)
        {
            player_pathfind_timer = 0;
            return;
        }
        FollowPath();
    }

    //AI state that handles what happens when the enemy is wandering aimlessly
    void WanderLogic()
    {
        //if we haven't already started wandering, find a nearby connected node and pathfind to it
        if (!wandering)
        {
            int start_index = -1;
            NavTree.NavNode start_node = nav_tree.GetClosestNodeToPoint(transform.position, ref start_index);

            //if there are no nearby nodes start ghosting towards the player
            if(start_index == -1)
            {
                ghost = true;
                SetGhostDestination();
                in_wall = false;
                return;
            }

            List<NavTree.NavNode> connected_nodes = new List<NavTree.NavNode>();
            connected_nodes = nav_tree.GetChunk(transform.position).GetAllConnectedNodes(start_index);

            int random_node = Random.Range(0, connected_nodes.Count);
            Pathfind(transform.position, connected_nodes[random_node].position);
            wandering = true;
        }
        //if we are wandering, simply follow the path
        else
        {
            if(path_queue.Count == 0)
            {
                int start_index = -1;
                NavTree.NavNode start_node = nav_tree.GetClosestNodeToPoint(transform.position, ref start_index);

                //if there are no nearby nodes start ghosting towards the player
                if (start_index == -1)
                {
                    ghost = true;
                    SetGhostDestination();
                    in_wall = false;
                    return;
                }

                List<NavTree.NavNode> connected_nodes = new List<NavTree.NavNode>();
                connected_nodes = nav_tree.GetChunk(transform.position).GetAllConnectedNodes(start_index);

                int random_node = Random.Range(0, connected_nodes.Count);
                Pathfind(transform.position, connected_nodes[random_node].position);
            }

            FollowPath();
        }
    }

    //Function readies a destination for the enemy before ghosting
    void SetGhostDestination()
    {
        int destination_index = -1;
        Vector3 destination = GameObject.FindGameObjectWithTag("Player").transform.position;
        NavTree.NavNode destination_node = nav_tree.GetClosestNodeToPoint(destination, ref destination_index);

        if(destination_node == null)
        {
            ghost_destination = destination;
            return;
        }
        ghost_destination = destination_node.position;
    }

    //AI state that handles what happens when the enemy is ghosting to a point
    void GhostLogic(Vector3 destination)
    {
        if (ghost_sound.volume != ghost_volume)
            ghost_sound.volume = ghost_volume;
        //disable the collider of the enemy
        GetComponent<Collider>().isTrigger = true;

        //move towards the destination
        rb.velocity = (destination - transform.position).normalized * move_speed * 0.5f;

        //if this enemy is in a wall then wait until it gets close enough to another pathfinding node
        if (in_wall) {
            int closest_index = -1;
            NavTree.NavNode closest_node = nav_tree.GetClosestNodeToPoint(transform.position, ref closest_index);

            if (closest_node == null)
                return;

            //when close enough to a pathfinding node, stop ghosting
            if (Vector3.Distance(transform.position, closest_node.position) < GetComponent<Collider>().bounds.extents.x / 1.25f ||
                Vector3.Distance(transform.position, destination) < GetComponent<Collider>().bounds.extents.x / 1.25f)
            {
                GetComponent<Collider>().isTrigger = false;
                ghost = false;
                in_wall = false;
                already_checked_exit = false;
                ghost_sound.volume = 0.0f;
            }
        }
    }

    //Function the player can use to pump an enemy
    public void Pump()
    {
        pump_level += 1;
        pump_timer = pump_timer_max;

        if (pump_level == 4 && !dead)
            StartCoroutine(DeathSequence());
    }

    //Function handles everything to do with this enemy when they are getting pumped
    void PumpLogic()
    {
        //Don't move if you're in the pump state also don't collide with the player
        if (pump_level > 0)
        {
            is_being_pumped = true;
            if (GameObject.FindGameObjectWithTag("Player") != null)
                Physics.IgnoreCollision(GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(), GetComponent<Collider>());
            rb.velocity = Vector3.zero;

            //Decrease the pump timer, when it hits 0, decrement the pump level and reset the timer
            if (pump_timer <= 0.0f)
            {
                pump_level--;
                pump_timer = pump_timer_max;
            }
            else
                pump_timer -= Time.deltaTime;

            return;
        }
        is_being_pumped = false;
        if (GameObject.FindGameObjectWithTag("Player") != null)
            Physics.IgnoreCollision(GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(), GetComponent<Collider>(), false);
    }

    //Overrideable function that determines the basic logic of an enemy
    public virtual void AI()
    {
        //don't do anything if you're squished
        if (squished)
        {
            if (GameObject.FindGameObjectWithTag("Player") != null)
                Physics.IgnoreCollision(GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(), GetComponent<Collider>());
            return;
        }

        //don't do anything if you're dead
        if (dead)
            return;

        PumpLogic();

        //avoid any pathfinding if this enemy can't get the nav tree or the terrain generator
        if (nav_tree == null || generator == null)
            return;

        //don't do any ai if we're being pumped
        if (is_being_pumped)
            return;

        //if this is the last enemy alive then do the escape logic
        if (last_enemy_flags[0] == true)
        {
            EscapeLogic();
            return;
        }

        //try to periodically update pathfinding to the player
        if (player_pathfind_timer <= 0 && !ghost && !wandering)
        {
            player_pathfind_timer = player_pathfind_timer_max;
            can_path_to_player = Pathfind(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position);
            boredom_counter--;
        }
        else
        {
            //if the enemy has been chasing the player too long, get bored and wander away from the player
            if (boredom_counter > 0)
            {
                ChaseLogic();
                player_pathfind_timer -= Time.deltaTime;
            }
            else
            {
                boredom_counter = boredom_max;
                can_path_to_player = false;
            }
        }

        //if this enemy cannot pathfind to the player, then wander for a certain amount of time then ghost to the player
        if (!can_path_to_player)
        {
            if (!ghost)
            {
                if (wander_timer <= 0)
                {
                    ghost = true;
                    SetGhostDestination();
                    in_wall = false;
                    wandering = false;
                    wander_timer = wander_timer_max;
                }
                else
                {
                    WanderLogic();
                    wander_timer -= Time.deltaTime;
                }
            }
            else
                GhostLogic(ghost_destination);
        }
    }

    // Start is called before the first frame update
    public virtual void Start()
    {
        can_path_to_player = false;
        wandering = false;
        ghost = false;
        in_wall = false;
        already_checked_exit = false;
        can_reach_exit = false;
        reached_exit = false;
        squished = false;

        last_enemy_flags = new bool[4] { false, false, false, false };

        boredom_max = boredom_counter;

        player_pathfind_timer_max = player_pathfind_timer * Random.Range(0.5f, 1.5f);
        player_pathfind_timer = player_pathfind_timer_max;

        wander_timer_max = wander_timer * Random.Range(1f, 2f);
        wander_timer = wander_timer_max;

        pump_timer_max = pump_timer;
        pump_timer = 0.0f;
        pump_level = 0;

        starting_position = transform.position;

        if(GameObject.FindGameObjectWithTag("NavTree") != null)
            nav_tree = GameObject.FindGameObjectWithTag("NavTree").GetComponent<NavTree>();
        if(GameObject.FindGameObjectWithTag("TerrainGenerator") != null)
            generator = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<TerrainGeneration>();
        path_queue = new List<Vector3>();
        rb = GetComponent<Rigidbody>();
        ghost_sound = GetComponents<AudioSource>()[0];
        pop_sound = GetComponents<AudioSource>()[1];
    }

    //updates this object's echo filter based on where in the world the player is
    void UpdateEchoFilter()
    {
        AudioEchoFilter player_echo = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioEchoFilter>();
        GetComponent<AudioEchoFilter>().wetMix = player_echo.wetMix;
        GetComponent<AudioEchoFilter>().dryMix = player_echo.dryMix;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().IsReady())
            AI();
        else
            rb.velocity = Vector3.zero;

        UpdateEchoFilter();
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            in_wall = true;
    }
}
