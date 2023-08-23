using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockBehavior : MonoBehaviour
{
    public bool squashed_enemy, squashed_player;

    [SerializeField]
    private float bounce_epsilon;
    [SerializeField]
    private bool show_hitbox;
    private bool start_sequence;
    private bool hit_ground;
    private Vector3 highest_point;
    private BoxCollider col;
    private Rigidbody rb;
    private Animator anim;
    private AudioSource crush;

    [SerializeField]
    private GameObject kill_box, stop_box;

    //Function handles what the rock does when it actually begins to fall
    IEnumerator FallSequence()
    {
        //first wobble the rock
        anim.SetBool("Wobble", true);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("Wobble"));
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);

        //stop wobbling, and enter the fall state
        anim.SetBool("Wobble", false);
        rb.useGravity = true;
        kill_box.SetActive(true);
        stop_box.SetActive(true);

        yield return new WaitUntil(() => hit_ground || squashed_enemy || squashed_player);

        //if this rock squashed something, then simply wait for the stop box to intersect terrain before killing all squashed enemies and disappearing after a short bit
        if(squashed_enemy == true || squashed_player == true)
        {
            yield return new WaitUntil(() => stop_box.GetComponent<RockStopbox>().stop);
            crush.Play();
            rb.useGravity = false;
            rb.velocity = Vector3.zero;

            yield return new WaitForSeconds(1.0f);

            //first kill all enemies to apply the score
            if(squashed_enemy)
                kill_box.GetComponent<RockKillbox>().CalculateSquishedScore();

            //then kill the player if the player was squished
            if (squashed_player)
            {
                PlayerMovement player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
                if (!player.GetDying())
                    player.Die();
            }
            DestructionSequence();
            yield return null;
        }

        //if this rock hit nothing, then wait for the rock to collide with the ground, then break
        crush.Play();
        anim.SetBool("Break", true);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("Break"));
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        DestructionSequence();    
    }

    //Helper function to avoid warnings
    void DestructionSequence()
    {
        if (GameManager.instance != null)
            GameManager.instance.rocks_dropped++;
        StopCoroutine(FallSequence());
        Destroy(gameObject);
    }

    //Function will boxcast down looking for the highest point underneath it.  If it changes, and the player is not blocking this rock, then start to fall
    //returns false if not ready to fall, returns true if ready to fall
    bool CheckForFall()
    {
        RaycastHit hit = new RaycastHit();
        bool hit_something = false;
        LayerMask rock_mask = LayerMask.GetMask("Player", "Terrain");
        Vector3 bbox_half_extents = col.bounds.extents;
        bbox_half_extents.y = 0.1f;
        hit_something = Physics.BoxCast(transform.position + Vector3.up * 0.2f, bbox_half_extents, Vector3.down, out hit, Quaternion.identity, float.MaxValue, rock_mask);

        if (show_hitbox) {
            //draw the collision box being used for calculations
            Vector3 half_vector = hit.point - (transform.position + Vector3.up * 0.2f);
            half_vector.x = 0.0f;
            half_vector.y /= 2.0f;
            half_vector.z = 0.0f;

            Vector3 half_extents = col.bounds.extents;
            half_extents.y = half_vector.y;

            ExtDebug.DrawBox(transform.position + half_vector + Vector3.up * 0.1f, half_extents, Quaternion.identity, Color.blue);

            //draw the hit location
            Debug.DrawRay(hit.point, Vector3.right * 0.25f, Color.red);
            Debug.DrawRay(hit.point, Vector3.up * 0.25f, Color.green);
            Debug.DrawRay(hit.point, Vector3.forward * 0.25f, Color.blue);
        }

        //if there is nothing to hit and the rock has been activated
        if (!hit_something && highest_point != Vector3.one * -float.MaxValue)
            return true;

        //don't calculate if there is a small change in height between the highest point and the hit point
        if (Mathf.Abs(hit.point.y - highest_point.y) < bounce_epsilon)
            return false;

        //always set the highest point to the highest collision
        if(highest_point.y < hit.point.y && hit_something)
        {
            highest_point = hit.point;
            return false;
        }

        //if the highest point is higher than the current point
        if (hit.point.y < highest_point.y)
            return true;

        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        squashed_enemy = false;
        squashed_player = false;
        start_sequence = false;
        hit_ground = false;
        highest_point = Vector3.one * -float.MaxValue;
        col = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        crush = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (CheckForFall() && !start_sequence)
        {
            start_sequence = true;
            StartCoroutine(FallSequence());
        }

        //update the echo filter of this object to match the player
        AudioEchoFilter player_echo = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioEchoFilter>();
        GetComponent<AudioEchoFilter>().wetMix = player_echo.wetMix;
        GetComponent<AudioEchoFilter>().dryMix = player_echo.dryMix;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Terrain") || collision.gameObject.layer == LayerMask.NameToLayer("WorldBounds"))
            hit_ground = true;
    }
}
