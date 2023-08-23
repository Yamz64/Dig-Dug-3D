using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockStopbox : MonoBehaviour
{
    public bool stop;

    private void Start()
    {
        stop = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            stop = true;
    }
}
