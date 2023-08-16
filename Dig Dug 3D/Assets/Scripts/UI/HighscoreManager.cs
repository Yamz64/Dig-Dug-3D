using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HighscoreManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField name_input;

    [SerializeField]
    private LoadLeaderboard leader_board;

    private bool high_score_set;
    private string last_valid_name;
    private char[] allowed_characters;

    //Function returns if the highscore has been set properly
    public bool HighScoreSet() { return high_score_set; }

    //Function does not allow the user to have more than 3 characters in the initials, and only allows capitals/period
    public void LimitCharacters()
    {
        name_input.text = name_input.text.ToString().ToUpper();

        //check for invalid characters
        string input_text = name_input.text.ToString();
        bool valid_name = true;
        for(int i=0; i<input_text.Length; i++)
        {
            valid_name = false;
            for(int j=0; j<allowed_characters.Length; j++)
            {
                if(input_text[i] == allowed_characters[j])
                {
                    valid_name = true;
                    break;
                }
            }

            if (!valid_name)
                break;
        }

        //check if the name is too long
        if (input_text.Length > 3)
            name_input.text = input_text.Substring(0, 3);

        if (!valid_name)
            name_input.text = last_valid_name;
        else
            last_valid_name = input_text;
    }

    //Function handles entering the final name as well as uploading it to the leaderboard
    public void FinalizeNameEntry()
    {
        if (name_input.text.Length == 3)
        {
            name_input.interactable = false;
            leader_board.WriteNameToLeaderboard(name_input.text);
            leader_board.SetInitials(name_input.text);
            leader_board.UploadToOnlineLeaderboard();
            StartCoroutine(FinalizeNameEntrySequence());
        }
    }

    //Function loads to the main menu when the final score has uploaded
    IEnumerator FinalizeNameEntrySequence()
    {
        //wait until uploaded and song has finished
        AudioSource source = GetComponent<AudioSource>();
        yield return new WaitUntil(() => leader_board.GetFinishedUpload());
        source.loop = false;
        yield return new WaitUntil(() => !source.isPlaying);
        yield return new WaitForSeconds(3.0f);
        high_score_set = true;
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        high_score_set = false;

        last_valid_name = "";
        allowed_characters = new char[27] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '.' };

        if(PlayerPrefs.HasKey("Music"))
            GetComponent<AudioSource>().volume *= PlayerPrefs.GetInt("Music") / 100.0f;
    }
}
