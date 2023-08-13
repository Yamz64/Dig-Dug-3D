using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxBehavior : MonoBehaviour
{
    private bool found_player;
    private Transform main_cam;

    IEnumerator StartSequence()
    {
        yield return new WaitUntil(() => Camera.main != null);
        main_cam = Camera.main.transform;
        found_player = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        found_player = false;
        StartCoroutine(StartSequence());
    }

    // Update is called once per frame
    void Update()
    {
        if (found_player)
            transform.rotation = main_cam.rotation;
    }
}
