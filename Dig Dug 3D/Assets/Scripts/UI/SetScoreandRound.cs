using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetScoreandRound : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI score, round;

    // Start is called before the first frame update
    void Start()
    {
        score.text = GameManager.instance.score.ToString();
        round.text = GameManager.instance.level.ToString();
    }
}
