using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockKillbox : MonoBehaviour
{
    private List<GameObject> squashed_objects;
    private RockBehavior rock;
    private AudioSource squish;

    //Adds an enemy to the squished list of objects, to be destroyed later, if it is not already in the squished list of objects
    void AddSquashed(GameObject new_squished)
    {
        foreach(GameObject enemy in squashed_objects)
        {
            if (enemy == new_squished)
                return;
        }
        squashed_objects.Add(new_squished);
    }

    public void CalculateSquishedScore()
    {
        int score_bonus = 0;
        switch (squashed_objects.Count)
        {
            case 0:
                score_bonus = 0;
                break;
            case 1:
                score_bonus = 1000;
                break;
            case 2:
                score_bonus = 2500;
                break;
            case 3:
                score_bonus = 4000;
                break;
            case 4:
                score_bonus = 6000;
                break;
            case 5:
                score_bonus = 8000;
                break;
            case 6:
                score_bonus = 10000;
                break;
            case 7:
                score_bonus = 12000;
                break;
            case 8:
                score_bonus = 15000;
                break;
            default:
                score_bonus = 15000;
                break;
        }

        if (GameManager.instance != null)
            GameManager.instance.score += score_bonus;

        foreach (GameObject enemy in squashed_objects)
        {
            Destroy(enemy);
            if (GameManager.instance != null)
                GameManager.instance.alive_enemies--;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        squashed_objects = new List<GameObject>();
        rock = transform.parent.GetComponent<RockBehavior>();
        squish = GetComponent<AudioSource>();
    }

    private void Update()
    {
        AudioEchoFilter player_echo = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioEchoFilter>();
        GetComponent<AudioEchoFilter>().wetMix = player_echo.wetMix;
        GetComponent<AudioEchoFilter>().dryMix = player_echo.dryMix;
    }

    private void OnTriggerEnter(Collider other)
    {
        //squished enemy
        if(other.tag == "Enemy")
        {
            squish.Play();
            other.transform.parent = transform.parent;
            AddSquashed(other.gameObject);
            other.GetComponent<BaseEnemyAI>().SetSquished(true);
            rock.squashed_enemy = true;
        }

        if(other.tag == "Player")
        {
            squish.Play();
            rock.squashed_player = true;
            other.GetComponent<PlayerMovement>().LockMovement();
            other.GetComponent<PlayerMovement>().SetSquished(true);
        }
    }
}
