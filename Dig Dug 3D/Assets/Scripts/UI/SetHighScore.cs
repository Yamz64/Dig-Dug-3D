using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetHighScore : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey("Highscore"))
            PlayerPrefs.SetInt("Highscore", 10000);

        transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetInt("Highscore").ToString();
    }
}
