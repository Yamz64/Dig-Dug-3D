using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustFOV : MonoBehaviour
{
    [SerializeField]
    private string fov_preference;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey(fov_preference))
            GetComponent<Camera>().fieldOfView = PlayerPrefs.GetInt(fov_preference);
    }
}
