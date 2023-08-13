using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Dan.Main;
using Dan.Models;

public class LoadLeaderboard : MonoBehaviour
{
    private string leaderboard_public_key;
    private List<ScoreEntry> leader_board;

    class ScoreEntry
    {
        public ScoreEntry(TextMeshProUGUI score = null, TextMeshProUGUI round = null, TextMeshProUGUI name = null)
        {
            _score = score;
            _round = round;
            _name = name;
        }

        //SETTERS
        public void SetScore(int score) { _score.text = score.ToString(); }
        public void SetRound(int round) { _round.text = round.ToString(); }
        public void SetName(string name) { _name.text = name; }

        //ACCESSORS
        public int GetScore() { return int.Parse(_score.text); }
        public int GetRound() { return int.Parse(_round.text); }
        public string GetName() { return _name.text; }

        TextMeshProUGUI _score, _round, _name;
    }

    //Function gets references to all of the local leaderboard objects
    void LoadLocalLeaderboard()
    {
        leader_board = new List<ScoreEntry>();
        
        for(int i=1; i<transform.childCount; i++)
        {
            TextMeshProUGUI score = transform.GetChild(i).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI round = transform.GetChild(i).transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI name = transform.GetChild(i).transform.GetChild(3).GetComponent<TextMeshProUGUI>();

            ScoreEntry temp_entry = new ScoreEntry(score, round, name);
            leader_board.Add(temp_entry);
        }
    }

    //Function loads the online leaderboard
    public void LoadOnlineLeaderboard() => LeaderboardCreator.GetLeaderboard(leaderboard_public_key, OnLeaderboardLoaded);

    //Function populates data when the leaderboard is loaded
    void OnLeaderboardLoaded(Entry[] entries)
    {
        if(entries.Length == 0)
        {
            Debug.LogWarning("No entries in the online leaderboard!");
            return;
        }

        if (entries.Length != leader_board.Count)
        {
            Debug.LogWarning("Online leaderboard does not match the size of the local leaderboard!");
            return;
        }

        for(int i=0; i<leader_board.Count; i++)
        {
            //parse the round
            string entry_extra = entries[i].Extra;
            entry_extra = entry_extra.Substring(7);

            int round = int.Parse(entry_extra);

            //load that to the ui
            leader_board[i].SetScore(entries[i].Score);
            leader_board[i].SetRound(round);
            leader_board[i].SetName(entries[i].Username);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        leaderboard_public_key = "ac8601e7913c6dc8376e5b79d95f7d8344d587e2a0d0305dcf3012166f787c46";
        LoadLocalLeaderboard();
        LoadOnlineLeaderboard();
    }
}
