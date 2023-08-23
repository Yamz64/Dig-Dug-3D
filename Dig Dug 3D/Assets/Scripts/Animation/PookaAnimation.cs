using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PookaAnimation : MonoBehaviour
{
    private Rigidbody rb;
    private Animator anim;
    [SerializeField]
    private Vector3 look_dir;
    private BaseEnemyAI ai;

    //function will calculate a direction this enemy should be facing based off from the way it's moving
    void HandleDirection()
    {
        //first, if we're moving, update the way we're looking to that vector
        Vector3 move_vector = rb.velocity;
        move_vector.y = 0;

        if (move_vector.normalized.magnitude != 0)
            look_dir = move_vector.normalized;

        //see what direction is closest to our move vector
        Vector3[] possible_directions = new Vector3[8] {new Vector3(0, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, 0),
                                                        new Vector3(-1, 0, -1), new Vector3(0, 0, -1), new Vector3(1, 0, -1),
                                                        new Vector3(1, 0, 0), new Vector3(1, 0, 1)};

        float closest_projection = Mathf.Infinity;
        int closest_direction = int.MaxValue;
        for(int i=0; i<possible_directions.Length; i++)
        {
            float this_projection = 1.0f - Vector3.Dot(possible_directions[i].normalized, look_dir);
            if(this_projection < closest_projection)
            {
                closest_projection = this_projection;
                closest_direction = i;
            }
        }

        anim.SetFloat("Direction", closest_direction);
    }

    //Function updates between walking, ghosting, and inflating
    void UpdateAnimationState()
    {
        if (ai.GetSquished())
            anim.SetBool("Squish", true);

        anim.SetInteger("Inflate", ai.GetPumpLevel());
        if (ai.GetGhost())
        {
            anim.SetBool("Walk", false);
            anim.SetBool("Ghost", true);
        }
        else
        {
            anim.SetBool("Walk", true);
            anim.SetBool("Ghost", false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = transform.parent.GetComponent<Rigidbody>();
        ai = transform.parent.GetComponent<BaseEnemyAI>();
        anim = GetComponent<Animator>();
        look_dir = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        HandleDirection();
        UpdateAnimationState();
    }
}
