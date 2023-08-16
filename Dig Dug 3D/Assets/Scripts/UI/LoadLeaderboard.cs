using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Dan.Main;
using Dan.Models;

public class LoadLeaderboard : MonoBehaviour
{
    private int beaten_index;
    private bool entry_mode, finished_upload;
    private string leaderboard_public_key, initials;
    private List<ScoreEntry> leader_board;

    class ScoreEntry
    {
        public ScoreEntry(TextMeshProUGUI score = null, TextMeshProUGUI round = null, TextMeshProUGUI name = null, TextMeshProUGUI position = null)
        {
            _score = score;
            _round = round;
            _name = name;
            _position = position;
        }

        //SETTERS
        public void SetScore(int score) { _score.text = score.ToString(); }
        public void SetRound(int round) { _round.text = round.ToString(); }
        public void SetName(string name) { _name.text = name; }
        public void SetPosition(string position) { _position.text = position; }

        //ACCESSORS
        public int GetScore() { return int.Parse(_score.text); }
        public int GetRound() { return int.Parse(_round.text); }
        public string GetName() { return _name.text; }
        public string GetPosition() { return _position.text; }

        //MISC
        public void Highlight()
        {
            _score.color = new Color(0.9803922f, 1.0f, 0.0f);
            _round.color = new Color(0.9803922f, 1.0f, 0.0f);
            _name.color = new Color(0.9803922f, 1.0f, 0.0f);
            _position.color = new Color(0.9803922f, 1.0f, 0.0f);
        }

        TextMeshProUGUI _score, _round, _name, _position;
    }

    public void SetInitials(string i) { initials = i; }
    public bool GetFinishedUpload() { return finished_upload; }

    //Function gets references to all of the local leaderboard objects
    void LoadLocalLeaderboard()
    {
        leader_board = new List<ScoreEntry>();
        
        for(int i=1; i<transform.childCount; i++)
        {
            TextMeshProUGUI score = transform.GetChild(i).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI round = transform.GetChild(i).transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI name = transform.GetChild(i).transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI position = transform.GetChild(i).transform.GetChild(0).GetComponent<TextMeshProUGUI>();

            ScoreEntry temp_entry = new ScoreEntry(score, round, name, position);
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

        //sort the entry array so that the top 5 entries are organized first
        Array.Sort(entries, (a, b) => b.Score.CompareTo(a.Score));

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

    //Function will load the online leaderboard except for the beaten entry
    public void LoadOnlineLeaderboardBeaten() => LeaderboardCreator.GetLeaderboard(leaderboard_public_key, OnLeaderboardLoadedBeaten);

    //Function populates data when the beaten leaderboard is loaded
    void OnLeaderboardLoadedBeaten(Entry[] entries)
    {
        if (entries.Length == 0)
        {
            Debug.LogWarning("No entries in the online leaderboard!");
            return;
        }

        //sort the entry array so that the top 5 entries are organized first
        Array.Sort(entries, (a, b) => b.Score.CompareTo(a.Score));

        //get the game manager if it's active
        GameManager manager = GameManager.instance;
        if(manager == null)
        {
            Debug.LogWarning("No manager detected!");
            return;
        }

        beaten_index = -1;
        for (int i = 0; i < leader_board.Count; i++)
        {
            //if the highscore is higher than this position on the leaderboard, then replace it on the scoreboard and skip
            if(beaten_index == -1 && manager.highscore > entries[i].Score)
            {
                beaten_index = i;
                leader_board[i].SetScore(manager.highscore);
                leader_board[i].SetRound(manager.level);
                leader_board[i].SetName("");
                leader_board[i].Highlight();
                continue;
            }

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

    //Function will add a score to the global leaderboard
    public void UploadToOnlineLeaderboard() => LeaderboardCreator.UploadNewEntry(leaderboard_public_key, initials, GameManager.instance.score, $"Round: {GameManager.instance.level}", OnUploadComplete);

    //function waits until the score has been uploaded to the leaderboard, then reports a finished upload whether it fails or succeeds, for offline play
    void OnUploadComplete(bool success)
    {
        finished_upload = true;
    } 

    public void WriteNameToLeaderboard(string name)
    {
        leader_board[beaten_index].SetName(name);
    }

    // Start is called before the first frame update
    void Start()
    {
        finished_upload = false;
        entry_mode = SceneManager.GetActiveScene().name == "HighScoreEntry";
        leaderboard_public_key = "ac8601e7913c6dc8376e5b79d95f7d8344d587e2a0d0305dcf3012166f787c46";
        initials = "";
        LoadLocalLeaderboard();

        if (!entry_mode)
            LoadOnlineLeaderboard();
        else
            LoadOnlineLeaderboardBeaten();
    }
}
