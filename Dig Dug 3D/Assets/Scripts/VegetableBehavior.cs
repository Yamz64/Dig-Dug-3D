using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VegetableBehavior : MonoBehaviour
{
    public float life_time;

    //vegetable textures, sprites, and models are parallel arrays holding respective data using the following index codes:
    // 0: carrot, 1: turnip, 2: mushroom, 3: cucumber, 4: eggplant, 5: pepper, 6: tomato, 7: garlic, 8: watermelon, 9: galaxian, 10: pineapple
    [SerializeField]
    private Texture[] vegetable_textures;
    [SerializeField]
    private Sprite[] vegetable_sprites;
    [SerializeField]
    private Mesh[] vegetable_models;

    private int _vegetable_index;
    private bool dying;

    private MeshRenderer m_rend;
    private MeshFilter m_filter;
    private AudioSource source;
    private Image vegetable_waypoint;


    //Function handles the normal, uninterrupted death sequence
    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(life_time);

        if (vegetable_waypoint != null)
            vegetable_waypoint.enabled = false;

        Destroy(gameObject);
    }

    //Function Handles Picking up the Vegetable
    IEnumerator PickUpSequence()
    {
        StopCoroutine(DeathSequence());
        dying = true;
        m_rend.enabled = false;
        source.Play();
        ApplyScore();

        if (vegetable_waypoint != null)
            vegetable_waypoint.enabled = false;

        yield return new WaitUntil(() => !source.isPlaying);
        Destroy(gameObject);
    }

    //Function will attempt to apply score based on the current vegetable
    public void ApplyScore()
    {
        if (GameManager.instance == null)
            return;

        switch (_vegetable_index)
        {
            case 0:
                GameManager.instance.score += 400;
                break;
            case 1:
                GameManager.instance.score += 600;
                break;
            case 2:
                GameManager.instance.score += 800;
                break;
            case 3:
                GameManager.instance.score += 1000;
                break;
            case 4:
                GameManager.instance.score += 2000;
                break;
            case 5:
                GameManager.instance.score += 3000;
                break;
            case 6:
                GameManager.instance.score += 4000;
                break;
            case 7:
                GameManager.instance.score += 5000;
                break;
            case 8:
                GameManager.instance.score += 6000;
                break;
            case 9:
                GameManager.instance.score += 7000;
                break;
            case 10:
                GameManager.instance.score += 8000;
                break;
            default:
                GameManager.instance.score += 400;
                break;
        }
    }

    //Function will attempt to set the vegetable based on the current level
    public void SetVegetable()
    {
        if (GameManager.instance == null)
            return;

        if (GameManager.instance.level >= 18)
        {
            vegetable = 10;
            return;
        }
        if (GameManager.instance.level >= 16)
        {
            vegetable = 9;
            return;
        }
        if (GameManager.instance.level >= 14)
        {
            vegetable = 8;
            return;
        }
        if (GameManager.instance.level >= 12)
        {
            vegetable = 7;
            return;
        }
        if (GameManager.instance.level >= 10)
        {
            vegetable = 6;
            return;
        }
        if (GameManager.instance.level >= 8)
        {
            vegetable = 5;
            return;
        }
        if (GameManager.instance.level >= 6)
        {
            vegetable = 4;
            return;
        }
        if (GameManager.instance.level >= 4)
        {
            vegetable = 3;
            return;
        }

        for(int i=1; i<=3; i++)
        {
            if (GameManager.instance.level == i)
                vegetable = i - 1;
        }
    }

    public void UpdateVegetableWaypoint()
    {
        float min_x = vegetable_waypoint.GetPixelAdjustedRect().width / 2.0f;
        float max_x = Screen.width - min_x;

        float min_y = vegetable_waypoint.GetPixelAdjustedRect().height / 2.0f;
        float max_y = Screen.height - min_y;

        Vector2 waypoint_position = Camera.main.WorldToScreenPoint(transform.position);

        waypoint_position.x = Mathf.Clamp(waypoint_position.x, min_x, max_x);
        waypoint_position.y = Mathf.Clamp(waypoint_position.y, min_y, max_y);

        if(Vector3.Dot(transform.position - Camera.main.transform.position, Camera.main.transform.forward) < 0)
        {
            if (waypoint_position.x > Screen.width / 2.0f)
                waypoint_position.x = max_x;
            else
                waypoint_position.x = min_x;
        }

        vegetable_waypoint.transform.position = waypoint_position;
    }

    //public variable is useful for setting the vegetable from outside sources
    public int vegetable
    {
        get { return _vegetable_index; }
        set { 
            _vegetable_index = value;
            if (_vegetable_index < 0)
                _vegetable_index = 0;
            if (_vegetable_index > 10)
                _vegetable_index = 0;

            m_rend.material.SetTexture("_MainTex", vegetable_textures[_vegetable_index]);
            m_filter.mesh = vegetable_models[_vegetable_index];

            if (vegetable_waypoint != null)
            {
                vegetable_waypoint.sprite = vegetable_sprites[_vegetable_index];
                vegetable_waypoint.enabled = true;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        dying = false;
        m_rend = GetComponent<MeshRenderer>();
        m_filter = GetComponent<MeshFilter>();
        source = GetComponent<AudioSource>();
        if(GameObject.FindGameObjectWithTag("VegetableWaypoint") != null)
            vegetable_waypoint = GameObject.FindGameObjectWithTag("VegetableWaypoint").GetComponent<Image>();
        SetVegetable();
        StartCoroutine(DeathSequence());
    }

    private void Update()
    {
        if(vegetable_waypoint != null)
        {
            if (vegetable_waypoint.enabled)
                UpdateVegetableWaypoint();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            if (!dying)
                StartCoroutine(PickUpSequence());
        }
    }
}
