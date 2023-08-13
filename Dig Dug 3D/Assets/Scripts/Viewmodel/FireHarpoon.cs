using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireHarpoon : MonoBehaviour
{
    public float harpoon_velocity;
    [SerializeField]
    private float harpoon_offset, harpoon_timer, pump_timer;
    private float harpoon_timer_max, pump_timer_max;
    private bool pumping;
    [SerializeField]
    private GameObject harpoon, harpoon_segment;
    private GameObject harpoon_instance;
    private HarpoonBehavior harpoon_state;
    private AudioSource harpoon_sound, pump_sound;
    private PlayerMovement movement;
    private Transform viewmodel_cam;

    //helper function to see if the player is firing a harpoon
    public bool GetFiring() { return harpoon_timer > 0; }

    //helper function to see if the player is pumping
    public bool GetPumping() { return pumping; }

    //helper function to see if the player can pump
    public bool CanPump() { return pump_timer <= 0.0f; }

    //Function handles all logic surrounding firing the harpoon
    void Fire()
    {
        //if the harpoon dies early, then handle that
        if (harpoon == null)
            harpoon_timer = 0;

        //cleanup any harpoons
        if (harpoon_timer <= 0)
        {
            movement.lock_movement = false;
            if (harpoon_instance != null)
            {
                Destroy(harpoon_instance);
                harpoon_instance = null;
            }
        }
        else
            movement.lock_movement = true;

        //see if we should fire a harpoon, only fire if we're ready
        if (Input.GetMouseButtonDown(0))
        {
            if (harpoon_timer <= 0)
            {
                harpoon_timer = harpoon_timer_max;
                harpoon_sound.Play();
                Vector3 harpoon_spawn = viewmodel_cam.GetChild(0).position + viewmodel_cam.forward * harpoon_offset;
                harpoon_instance = (GameObject)Instantiate(harpoon, harpoon_spawn, viewmodel_cam.rotation);
                harpoon_instance.GetComponent<Rigidbody>().AddForce(harpoon_instance.transform.forward * harpoon_velocity, ForceMode.Impulse);
                harpoon_state = harpoon_instance.GetComponent<HarpoonBehavior>();
            }
        }
        if (harpoon_timer > 0)
            harpoon_timer -= Time.deltaTime;
    }

    //Function handles all logic surrounding pumping
    void Pump()
    {
        //Check to see if the enemy has escaped, popped, or the player wants to move, break the line

        if (harpoon_state.pump_object == null)
        {
            harpoon_timer = 0.0f;
            Destroy(harpoon_instance);
            harpoon_instance = null;
            return;
        }

        if (harpoon_state.pump_object.GetComponent<BaseEnemyAI>().GetPumpLevel() == 0)
        {
            harpoon_timer = 0.0f;
            Destroy(harpoon_instance);
            harpoon_instance = null;
            return;
        }

        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.0f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.0f)
        {
            harpoon_timer = 0.0f;
            Destroy(harpoon_instance);
            harpoon_instance = null;
            return;
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Space))
        {
            harpoon_timer = 0.0f;
            Destroy(harpoon_instance);
            harpoon_instance = null;
            return;
        }

        //Decrement the harpoon timer, when it is 0 the player can pump an enemy
        if (pump_timer <= 0.0f)
        {
            if (Input.GetMouseButtonDown(0))
            {
                harpoon_state.pump_object.GetComponent<BaseEnemyAI>().Pump();
                pump_timer = pump_timer_max;
                if(harpoon_state.pump_object.GetComponent<BaseEnemyAI>().GetPumpLevel() != 4)
                    pump_sound.Play();
            }
        }
        else
            pump_timer -= Time.deltaTime;
    }

    //function handles adding a line that trails behind the harpoon itself
    void AddTrail()
    {
        if (harpoon_instance == null)
            return;
        //keep track of how far in front the harpoon is from the viewmodel to determine how many children it should have
        //if it has no children then it should spawn a tether earlier
        float viewmodel_distance = (harpoon_instance.transform.position - viewmodel_cam.transform.GetChild(0).position).magnitude;
        if(harpoon_instance.transform.childCount == 0)
        {
            if (viewmodel_distance > 0.4f)
                Instantiate(harpoon_segment, harpoon_instance.transform.position, harpoon_instance.transform.rotation, harpoon_instance.transform);
            return;
        }
        else
        {
            viewmodel_distance += 0.5f;
            viewmodel_distance /= (1.6f * harpoon_instance.transform.childCount);
            Vector3 harpoon_offset = harpoon_instance.transform.forward * 1.6f;
            if(viewmodel_distance >= 1.0f)
                Instantiate(harpoon_segment, harpoon_instance.transform.position - harpoon_offset, harpoon_instance.transform.rotation, harpoon_instance.transform);
        }
    }

    //function checks the state of the harpoon to know what to do next
    void CheckHarpoon()
    {
        if (harpoon_instance == null)
        {
            pumping = false;
            return;
        }

        //If the harpoon has hit terrain
        if (harpoon_state.kill)
        {
            harpoon_timer = 0.0f;
            Destroy(harpoon_instance);
            harpoon_instance = null;
            harpoon_sound.Stop();
            return;
        }

        //If the harpoon has hit an enemy
        if(harpoon_state.pump_object != null && !pumping)
        {
            harpoon_state.pump_object.GetComponent<BaseEnemyAI>().Pump();
            pump_timer = pump_timer_max;
            pumping = true;
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        harpoon_timer_max = harpoon_timer;
        harpoon_timer = 0.0f;
        pump_timer_max = pump_timer;
        harpoon_sound = GetComponents<AudioSource>()[2];
        pump_sound = GetComponents<AudioSource>()[3];
        movement = GetComponent<PlayerMovement>();
        viewmodel_cam = Camera.main.transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (movement.IsReady())
        {
            CheckHarpoon();
            if (!pumping)
                Fire();
            else
                Pump();
            AddTrail();
        }
    }
}
