using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempFlycam : MonoBehaviour
{
    public int erode_radius = 1;
    public float erode_gizmo_scale;
    public float move_speed, mouse_sens;
    public float block_removal_error;
    private float rot_x, rot_y;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //function for debugging if point to voxel calculations work properly
    void FireRay()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity))
        {
            if (hit.collider.transform.parent.name != "TerrainGenerator")
                return;

            //get the erode chunk and erode at the hit location (erode location is slightly extended to avoid voxel calculation errors
            int chunk_index = int.Parse(hit.collider.gameObject.name.Split(' ')[1]);
            Vector3 erode_location = hit.point + (hit.point - transform.position) * block_removal_error;

            //erode a sphere around the erode location
            hit.collider.transform.root.GetComponent<TerrainGeneration>().ErodeChunkSphereAtLocation(chunk_index, erode_radius, erode_location);

            //draw a gizmo where the hit landed
            Debug.DrawRay(erode_location, Vector3.right * erode_gizmo_scale, Color.red, 3f);
            Debug.DrawRay(erode_location, Vector3.up * erode_gizmo_scale, Color.green, 3f);
            Debug.DrawRay(erode_location, Vector3.forward * erode_gizmo_scale, Color.blue, 3f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float vertical_move = 0.0f;
        if (Input.GetButton("Jump"))
            vertical_move = 1.0f;
        if (Input.GetKey(KeyCode.LeftControl))
            vertical_move -= 1.0f;

        transform.position += (transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal")).normalized * Time.deltaTime * move_speed;
        transform.position += Vector3.up * vertical_move * move_speed * Time.deltaTime;


        transform.rotation = Quaternion.identity;
        rot_x += mouse_sens * Input.GetAxis("Mouse X");
        rot_y += -mouse_sens * Input.GetAxis("Mouse Y");
        transform.Rotate(rot_y, rot_x, 0.0f);

        if (Input.GetKeyDown(KeyCode.E))
            FireRay();

        if (Input.GetAxis("Mouse ScrollWheel") > 0.0f)
            erode_radius++;
        if (Input.GetAxis("Mouse ScrollWheel") < 0.0f)
            erode_radius--;
    }
}
