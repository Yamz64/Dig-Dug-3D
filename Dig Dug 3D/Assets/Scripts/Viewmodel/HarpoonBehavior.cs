using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarpoonBehavior : MonoBehaviour
{
    public bool kill;
    public GameObject pump_object;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy")
        {
            //do not allow pumping of ghosting enemies
            if (other.gameObject.GetComponent<BaseEnemyAI>().GetGhost())
            {
                Physics.IgnoreCollision(other, GetComponent<Collider>());
                return;
            }
            GetComponent<Collider>().enabled = false;
            pump_object = other.gameObject;
            rb.velocity = Vector3.zero;
            return;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            kill = true;
    }
}
