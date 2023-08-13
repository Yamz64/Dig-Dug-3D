using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTouchKillbox : MonoBehaviour
{
    private BoxCollider parent_collider, kill_box;
    [SerializeField]
    private bool is_fygar_fire;

    // Start is called before the first frame update
    void Start()
    {
        if(transform.parent != null)
            parent_collider = transform.parent.GetComponent<BoxCollider>();

        kill_box = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        //always update this trigger to have a slightly larger collider than the parent's
        if (parent_collider != null)
        {
            if (kill_box.size != parent_collider.size)
                kill_box.size = parent_collider.size * 1.1f;
        }

        if (is_fygar_fire)
        {
            if (transform.childCount == 0)
                DestroyImmediate(gameObject);
        }
    }

    //Kill the player
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (is_fygar_fire)
            {
                if (!other.GetComponent<PlayerMovement>().GetDying())
                    other.GetComponent<PlayerMovement>().Die();
            }
            else
            {
                if(parent_collider != null)
                {
                    if(parent_collider.GetComponent<BaseEnemyAI>().GetPumpLevel() == 0) 
                    {
                        if (!other.GetComponent<PlayerMovement>().GetDying())
                            other.GetComponent<PlayerMovement>().Die();
                    }
                }
            }
        }
    }
}
