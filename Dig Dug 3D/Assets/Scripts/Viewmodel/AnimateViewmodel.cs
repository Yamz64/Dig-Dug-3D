using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateViewmodel : MonoBehaviour
{
    private bool shovel_mode = true;    //is the viewmodel a shovel or pump?
    [SerializeField]
    private Mesh shovel_model;
    [SerializeField]
    private Mesh[] pump_models;             //0 = up pump, 1 = down pump
    [SerializeField]
    private Material[] viewmodel_materials; //0 = shovel material, 1 = pump material
    private MeshFilter filter;
    private MeshRenderer rend;
    private PlayerMovement player;
    private FireHarpoon harpoon;

    //Logic for when the viewmodel is a shovel
    void Shovel()
    {
        //if the shovel isn't already being displayed, only display it if the player is digging and not shooting
        rend.enabled = player.GetDigging() && !harpoon.GetFiring();
    }

    //Logic for when the viewmodel is a pump
    void Pump()
    {if (harpoon.CanPump())
            filter.sharedMesh = pump_models[0];
        else
            filter.sharedMesh = pump_models[1];
    }

    // Start is called before the first frame update
    void Start()
    {
        filter = GetComponent<MeshFilter>();
        rend = GetComponent<MeshRenderer>();
        player = transform.root.GetComponent<PlayerMovement>();
        harpoon = transform.root.GetComponent<FireHarpoon>();
    }

    // Update is called once per frame
    void Update()
    {
        if (shovel_mode == harpoon.GetPumping())
        {
            shovel_mode = !harpoon.GetPumping();

            if (shovel_mode)
            {
                filter.sharedMesh = shovel_model;
                rend.material = viewmodel_materials[0];
            }
            else
            {
                rend.enabled = true;
                filter.sharedMesh = pump_models[0];
                rend.material = viewmodel_materials[1];
            }
        }

        if (shovel_mode)
            Shovel();
        else 
            Pump();
    }
}
