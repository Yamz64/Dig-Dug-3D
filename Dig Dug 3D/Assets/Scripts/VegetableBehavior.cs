using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetableBehavior : MonoBehaviour
{
    public float life_time;

    //vegetable textures and models are parallel arrays holding respective data using the following index codes:
    // 0: carrot, 1: turnip, 2: mushroom, 3: cucumber, 4: eggplant, 5: pepper, 6: tomato, 7: garlic, 8: watermelon, 9: galaxian, 10: pineapple
    [SerializeField]
    private Texture[] vegetable_textures;
    [SerializeField]
    private Mesh[] vegetable_models;

    private MeshRenderer m_rend;
    private MeshFilter m_filter;
    private AudioSource source;

    private int _vegetable_index;
    private bool dying;

    //Function handles the normal, uninterrupted death sequence
    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(life_time);
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
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        dying = false;
        m_rend = GetComponent<MeshRenderer>();
        m_filter = GetComponent<MeshFilter>();
        source = GetComponent<AudioSource>();
        SetVegetable();
        StartCoroutine(DeathSequence());
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
