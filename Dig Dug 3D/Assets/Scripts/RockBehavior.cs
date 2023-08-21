using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockBehavior : MonoBehaviour
{
    [SerializeField]
    private bool show_hitbox;
    private Vector3 highest_point;
    private BoxCollider col;

    //Function will boxcast down looking for the highest point underneath it.  If it changes, and the player is not blocking this rock, then start to fall
    //returns false if not ready to fall, returns true if ready to fall
    bool CheckForFall()
    {
        RaycastHit hit;
        Physics.BoxCast(transform.position, col.bounds.extents, Vector3.down, out hit);

        if (show_hitbox) {
            //draw the collision box being used for calculations
            Vector3 half_vector = hit.point - transform.position;
            half_vector.x = 0.0f;
            half_vector.y /= 2.0f;
            half_vector.z = 0.0f;

            Vector3 half_extents = col.bounds.extents;
            half_extents.y = half_vector.y;

            ExtDebug.DrawBox(transform.position + half_vector, half_extents, Quaternion.identity, Color.blue);

            //draw the hit location
            Debug.DrawRay(hit.point, Vector3.right * 0.25f, Color.red);
            Debug.DrawRay(hit.point, Vector3.up * 0.25f, Color.green);
            Debug.DrawRay(hit.point, Vector3.forward * 0.25f, Color.blue);
        }

        //if the highest point hasn't been defined yet
        if(highest_point == null)
        {
            highest_point = hit.point;
            return false;
        }

        if (hit.point.y < highest_point.y)
            return true;

        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckForFall();
    }
}
