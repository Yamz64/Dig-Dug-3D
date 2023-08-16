using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private bool starting_animation, dying;
    private bool dead;
    [HideInInspector]
    public bool lock_movement;
    [SerializeField]
    private float dig_speed, move_speed, mouse_sens, dig_slow_timer;
    [SerializeField]
    private float dig_range, block_removal_error, erode_gizmo_scale;
    [SerializeField]
    private float min_dry_mix, max_wet_mix;
    private float rot_x, rot_y;
    private float dig_slow_timer_max;
    private float dig_score_timer;
    [SerializeField]
    private int erode_radius;
    [SerializeField]
    private float[] layer_offsets;
    private Vector3 animation_dir;
    private Camera main_camera;
    private Rigidbody rb;
    [SerializeField]
    private AudioClip starting_jingle, walk_jingle;
    private AudioSource walk_sound, rock_break;
    private AudioEchoFilter echo_filter;
    [SerializeField]
    private Object terrain_knock_particle;
    [SerializeField]
    private Material ground_mat;
    private NavTree nav_tree;

    public void ResetCameraPosition() 
    {

        main_camera.transform.localPosition = new Vector3(0.0f, 0.25f, 0.0f);
        main_camera.transform.rotation = Quaternion.identity; 
    }

    public bool IsReady() { return !starting_animation && !dying; }

    public bool GetDigging() { return dig_slow_timer > 0; }

    public bool GetDying() { return dying; }

    public bool GetDead() { return dead; }

    public void LockMovement() { lock_movement = true; }

    public void UnlockMovement() { lock_movement = false; }

    public void SetDead(bool d) { dead = d; main_camera.GetComponent<Animator>().SetBool("Dead", false); }

    public void SetDying(bool d) { dying = d; }

    public float[] GetLayerOffsets() { return layer_offsets; }

    //function handles the starting animation
    IEnumerator StartingAnimation()
    {
        //move forward for 2.5 seconds
        animation_dir = Vector3.forward;
        yield return new WaitForSeconds(2.5f);
        //now move down until the y-position is close to 0
        animation_dir = -Vector3.up;
        yield return new WaitUntil(() => Mathf.Abs(transform.position.y) < 0.5f);
        //return control to the player after the jingle has finished
        animation_dir = Vector3.zero;
        yield return new WaitUntil(() => walk_sound.isPlaying == false);
        starting_animation = false;
        walk_sound.loop = true;
        walk_sound.clip = walk_jingle;
        walk_sound.volume = 0.0f;
        walk_sound.Play();
    }

    //function handles dying
    IEnumerator DyingAnimation()
    {
        main_camera.GetComponent<Animator>().SetBool("Dead", true);
        main_camera.GetComponent<AudioSource>().Play();
        yield return new WaitUntil(() => main_camera.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death"));
        yield return new WaitUntil(() => main_camera.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        dead = true;
    }

    //driver function for the death animation
    public void Die()
    {
        dying = true;
        StartCoroutine(DyingAnimation());
    }

    // Start is called before the first frame update
    void Start()
    {
        dig_slow_timer_max = dig_slow_timer;
        dig_slow_timer = 0.0f;

        dig_score_timer = 1.0f;

        main_camera = Camera.main;
        rot_x = main_camera.transform.rotation.eulerAngles.x;
        rot_y = main_camera.transform.rotation.eulerAngles.y;
        rb = GetComponent<Rigidbody>();
        walk_sound = GetComponents<AudioSource>()[0];
        rock_break = GetComponents<AudioSource>()[1];
        echo_filter = GetComponent<AudioEchoFilter>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.instance != null)
            ground_mat = GameManager.instance.GetGroundMaterial();

        if(GameObject.FindGameObjectWithTag("NavTree") != null)
            nav_tree = GameObject.FindGameObjectWithTag("NavTree").GetComponent<NavTree>();

        //get everything set up for the starting animation
        if (starting_animation)
        {
            //starting position
            rb.position -= Vector3.forward * (move_speed * 2.5f);
            walk_sound.clip = starting_jingle;
            walk_sound.loop = false;
            walk_sound.Play();
            StartCoroutine(StartingAnimation());
        }
    }

    //Function handles Camera Look Logic
    void Look()
    {
        rot_y += Input.GetAxis("Mouse X") * mouse_sens;
        rot_x -= Input.GetAxis("Mouse Y") * mouse_sens;

        rot_x = Mathf.Clamp(rot_x, -90.0f, 90.0f);
        main_camera.transform.localPosition = new Vector3(0.0f, 0.25f, 0.0f);
        main_camera.transform.rotation = Quaternion.Euler(rot_x, rot_y, 0.0f);
    }

    //Function handles spawning and coloring particles at a point
    void SpawnTerrainParticle(Vector3 position, Vector3 normal)
    {
        GameObject particle = (GameObject)Instantiate(terrain_knock_particle, position - normal * .75f, Quaternion.identity);
        particle.transform.forward = normal;


        Color particle_color_1, particle_color_2;
        if (position.y > layer_offsets[1])
        {
            particle_color_1 = ground_mat.GetColor("_TopsoilColor");
            particle_color_2 = ground_mat.GetColor("_OrganicColor");
        }
        else if (position.y > layer_offsets[2])
        {
            particle_color_1 = ground_mat.GetColor("_EluviationColor");
            particle_color_2 = ground_mat.GetColor("_TopsoilColor");
        }
        else if (position.y > layer_offsets[3])
        {
            particle_color_1 = ground_mat.GetColor("_SubsoilColor");
            particle_color_2 = ground_mat.GetColor("_EluviationColor");
        }
        else
        {
            particle_color_1 = ground_mat.GetColor("_ParentRockColor");
            particle_color_2 = ground_mat.GetColor("_SubsoilColor");
        }

        particle.GetComponent<AdjustTerrainKnockColor>().primary_color = particle_color_1;
        particle.GetComponent<AdjustTerrainKnockColor>().secondary_color = particle_color_2;
        rock_break.pitch = Random.Range(0.5f, 1.5f);
        rock_break.Play();
    }

    //Function handles digging
    void Dig(Vector3 dig_dir)
    {
        RaycastHit hit;
        //ignore the post processing layer
        LayerMask ignore_postprocessing = LayerMask.GetMask("Terrain");
        if (Physics.Raycast(transform.position, dig_dir, out hit, dig_range, ignore_postprocessing))
        {
            if (hit.collider.transform.parent.name != "TerrainGenerator")
                return;

            //get the erode chunk and erode at the hit location (erode location is slightly extended to avoid voxel calculation errors
            int chunk_index = int.Parse(hit.collider.gameObject.name.Split(' ')[1]);
            Vector3 erode_location = hit.point + (hit.point - transform.position) * block_removal_error;

            //erode a sphere around the erode location
            hit.collider.transform.root.GetComponent<TerrainGeneration>().ErodeChunkSphereAtLocation(chunk_index, erode_radius, erode_location);

            //spawn a particle effect
            SpawnTerrainParticle(hit.point, hit.normal);

            //make the player slower
            dig_slow_timer = dig_slow_timer_max;

            //add a pathfinding node for enemies
            if (nav_tree != null)
                nav_tree.AddNode(hit.point - hit.normal * block_removal_error);


            //draw a gizmo where the hit landed
            Debug.DrawRay(erode_location, Vector3.right * erode_gizmo_scale, Color.red, 3f);
            Debug.DrawRay(erode_location, Vector3.up * erode_gizmo_scale, Color.green, 3f);
            Debug.DrawRay(erode_location, Vector3.forward * erode_gizmo_scale, Color.blue, 3f);

        }
    }

    //Function handles Move Logic
    void Move()
    {
        Vector3 move_dir = main_camera.transform.right * Input.GetAxis("Horizontal") + main_camera.transform.forward * Input.GetAxis("Vertical");
        //try to move the player along their look direction projected along the xz axis
        if (!starting_animation)
        {
            move_dir.y = 0.0f;
            move_dir.Normalize();

            if (main_camera.transform.forward == Vector3.up)
                move_dir = -main_camera.transform.up * Input.GetAxis("Vertical");

            if (main_camera.transform.forward == Vector3.down)
                move_dir = main_camera.transform.up * Input.GetAxis("Vertical");

            if (Input.GetButton("Jump"))
                move_dir.y = 1.0f;
            if (Input.GetKey(KeyCode.LeftControl))
                move_dir.y -= 1.0f;
        }
        //special case for the starting animation
        else
            move_dir = animation_dir;

        //dig in the direction you're moving

        Dig(move_dir);

        if (dig_slow_timer <= 0)
            rb.velocity = move_dir.normalized * move_speed;
        else
        {
            rb.velocity = move_dir.normalized * dig_speed;
            if (starting_animation)
                rb.velocity *= 2.5f;
        }

        //play sound if you're moving
        if (!starting_animation)
        {
            if (rb.velocity.magnitude > 0.0)
                walk_sound.volume = 1.0f;
            else
                walk_sound.volume = 0.0f;
        }

        //adjust echo based off from how deep you are
        float deepness_factor = 1.0f - (transform.position.y - layer_offsets[4]) / (layer_offsets[0] - layer_offsets[4]);
        echo_filter.dryMix = Mathf.Lerp(1.0f, min_dry_mix, deepness_factor);
        echo_filter.wetMix = Mathf.Lerp(0.0f, max_wet_mix, deepness_factor);
    }

    // Update is called once per frame
    void Update()
    {
        if (!lock_movement && !dying)
        {
            Look();
            Move();
        }
        else
        {
            rb.velocity = Vector3.zero;
            walk_sound.volume = 0.0f;
        }

        //decrement the dig timer
        if (dig_slow_timer > 0)
        {
            dig_slow_timer -= Time.deltaTime;

            //decrement the dig score timer
            if ((new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"))).magnitude > 0.0f && !starting_animation)
                dig_score_timer -= Time.deltaTime;
        }

        //award 10 points for every second digging
        if(dig_score_timer <= 0.0f)
        {
            GameManager.instance.score += 10;
            dig_score_timer = 1.0f;
        }

        /* CHEAT INPUT FOR HIGHSCORE ENTRY
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            GameManager.instance.lives = 1;
            GameManager.instance.score = GameManager.instance.highscore + 1;
            dead = true;
        }
        */
    }
}
