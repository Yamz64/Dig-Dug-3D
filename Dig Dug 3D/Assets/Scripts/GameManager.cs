using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Dan.Main;
using Dan.Models;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    struct Palette
    {
        public Color organic_color;
        public Color topsoil_color;
        public Color eluviation_color;
        public Color subsoil_color;
        public Color parent_rock_color;
    }


    public static GameManager instance;
    [SerializeField]
    private int _score, _highscore, _level, _lives, _alive_enemies, _palette_index, _rocks_dropped;
    private int[] top_5;
    private string leaderboard_public_key;
    private bool next_level, high_score_broken, game_over, first_bonus, second_bonus;
    private GameObject ui;
    private GameObject[] ui_elements;                   //0 = score, 1 = high score, 2 = lives, 3 = flowers, 4 = round
    [SerializeField]
    private GameObject flower_prefab, vegetable;
    [SerializeField]
    private Sprite[] flower_sprites;                    //0 = white, 1 = yellow, 2 = red
    private AudioSource end_sound;
    [SerializeField]
    private AudioClip[] end_clips;                      //0 = escaping, 1 = completed, 2 = last one walk!, 3 = lose, 4 = new high score!, 5 = 1UP
    [SerializeField]
    private List<Palette> ground_palettes;
    [SerializeField]
    private Material ground_mat_instance;

    IEnumerator RespawnRoutine(GameObject player)
    {
        yield return new WaitForSeconds(2.5f);
        player.GetComponent<PlayerMovement>().SetDying(false);
        player.GetComponent<PlayerMovement>().UnlockMovement();
    }

    //Function handles all logic when a game should finish
    IEnumerator FinishGame()
    {
        //play the game over jingle, wait until it's finished, then follow the proper scene loading logic
        //load the highest score scene if the player has the highest score on this machine
        //load the high score entry scene if the player has a top 5 score
        //load into the main menu after everything is done
        end_sound.clip = end_clips[3];
        end_sound.Play();
        yield return new WaitUntil(() => end_sound.isPlaying);
        yield return new WaitUntil(() => !end_sound.isPlaying);

        //high score scene
        if (high_score_broken)
        {
            AsyncOperation next_scene = SceneManager.LoadSceneAsync("HighestScore");
            yield return new WaitUntil(() => next_scene.isDone);
            end_sound.clip = end_clips[4];
            end_sound.Play();
            yield return new WaitUntil(() => end_sound.isPlaying);
            yield return new WaitUntil(() => !end_sound.isPlaying);
        }

        //name entry scene
        if (IsTop5())
        {
            AsyncOperation next_scene = SceneManager.LoadSceneAsync("HighScoreEntry");
            yield return new WaitUntil(() => next_scene.isDone);
            HighscoreManager high_score_manager = GameObject.FindGameObjectWithTag("HighScoreManager").GetComponent<HighscoreManager>();
            yield return new WaitUntil(() => high_score_manager.HighScoreSet());
        }

        AsyncOperation title_screen = SceneManager.LoadSceneAsync("MainMenu");
        yield return new WaitUntil(() => title_screen.isDone);
        Destroy(gameObject);
    }

    void RespawnPlayer(GameObject player)
    {
        lives--;

        //see if the player has lost the game
        if(lives <= 0 && !game_over)
        {
            game_over = true;
            StartCoroutine(FinishGame());
            return;
        }

        if (game_over)
            return;


        //set the player at the start
        player.GetComponent<Rigidbody>().position = Vector3.zero;
        player.GetComponent<PlayerMovement>().SetDead(false);
        player.GetComponent<PlayerMovement>().SetSquished(false);
        player.GetComponent<PlayerMovement>().ResetCameraPosition();
        player.GetComponent<PlayerMovement>().LockMovement();

        //reset all the enemies to their starting locations
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
            enemies[i].GetComponent<BaseEnemyAI>().ResetAI();

        //wait 2.5 seconds before starting the game again
        StartCoroutine(RespawnRoutine(player));
    }

    IEnumerator LastEnemyRoutine()
    {
        next_level = false;
        StartCoroutine(LoadNextScene());
        //wait until 1 enemy remains
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length > 0);
        _alive_enemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        yield return new WaitUntil(() => _alive_enemies == 1);

        //mark the last enemy as ready to escape and wait for it to have escaped or died
        BaseEnemyAI enemy = GameObject.FindGameObjectWithTag("Enemy").GetComponent<BaseEnemyAI>();
        end_sound.clip = end_clips[0];
        end_sound.Play();
        GameObject.FindGameObjectWithTag("Player").GetComponents<AudioSource>()[0].Stop();
        yield return new WaitUntil(() => !end_sound.isPlaying);
        GameObject.FindGameObjectWithTag("Player").GetComponents<AudioSource>()[0].clip = end_clips[2];
        GameObject.FindGameObjectWithTag("Player").GetComponents<AudioSource>()[0].Play();
        yield return new WaitUntil(() => !enemy.GetGhost());
        enemy.Escape();
        yield return new WaitUntil(() => enemy.Escaped() || _alive_enemies == 0);
        end_sound.clip = end_clips[1];
        end_sound.Play();
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().SetDying(true);
        yield return new WaitUntil(() => !end_sound.isPlaying);
        next_level = true;
    }

    //Function loads the next scene to avoid lag
    IEnumerator LoadNextScene()
    {
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene("GameSceneNoCutscene", LoadSceneMode.Additive);
        yield return new WaitUntil(() => next_level == true);

        AsyncOperation unloaded_scene = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        while (!unloaded_scene.isDone)
            yield return null;

        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") == null);
        GameObject.FindGameObjectWithTag("Halter").GetComponent<Halter>().start = true;
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") != null);
        ReloadUI();
        level++;
        UpdateColors();
        StartCoroutine(LastEnemyRoutine());
    }

    //Function will get the top 5 scores to compare the highscore with later
    public void LoadTop5() => LeaderboardCreator.GetLeaderboard(leaderboard_public_key, InitializeTop5);

    //Function initializes the top 5 score once they have loaded
    void InitializeTop5(Entry[] entries)
    {
        top_5 = new int[5];

        for (int i = 0; i < top_5.Length; i++)
            top_5[i] = entries[i].Score;
    }

    //Function determines whether the highscore is a leaderboard score
    bool IsTop5()
    {
        for(int i=0; i<top_5.Length; i++)
        {
            if (score > top_5[i])
                return true;
        }
        return false;
    }

    public void UpdateColors()
    {
        if ((level - 1) % 4 == 0)
            _palette_index++;

        if (_palette_index >= ground_palettes.Count)
            _palette_index = 0;

        ground_mat_instance.SetColor("_OrganicColor", ground_palettes[_palette_index].organic_color);
        ground_mat_instance.SetColor("_TopsoilColor", ground_palettes[_palette_index].topsoil_color);
        ground_mat_instance.SetColor("_EluviationColor", ground_palettes[_palette_index].eluviation_color);
        ground_mat_instance.SetColor("_SubsoilColor", ground_palettes[_palette_index].subsoil_color);
        ground_mat_instance.SetColor("_ParentRockColor", ground_palettes[_palette_index].parent_rock_color);
    }

    public Material GetGroundMaterial() { return ground_mat_instance; }

    public int score
    {
        get { return _score; }
        set { 
            _score = value;

            //award a 1up if the player reaches a first and second bonus threshold
            if(_score >= 20000 && !first_bonus)
            {
                first_bonus = true;
                lives++;
                end_sound.clip = end_clips[5];
                end_sound.Play();
            }

            if (_score >= 60000 && !second_bonus)
            {
                second_bonus = true;
                lives++;
                end_sound.clip = end_clips[5];
                end_sound.Play();
            }

            if (_score > _highscore)
            {
                high_score_broken = true;
                highscore = _score;
            }

            if(ui != null)
            {
                ui_elements[0].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = _score.ToString();
            }
        }
    }

    public int highscore
    {
        get { return _highscore; }
        set { 
            _highscore = value;
            PlayerPrefs.SetInt("Highscore", _highscore);

            if(ui != null)
            {
                ui_elements[1].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = _highscore.ToString();
            }
        }
    }

    public int level
    {
        get { return _level; }
        set { 
            _level = value; 

            if(ui != null)
            {
                //set flowers first
                while (ui_elements[3].transform.childCount > 0)
                    DestroyImmediate(ui_elements[3].transform.GetChild(0).gameObject);

                //spawn 10 flowers first
                int round_temp = value;
                bool yellow = true;
                while(round_temp >= 10)
                {
                    GameObject temp_flower = (GameObject)Instantiate(flower_prefab, ui_elements[3].transform);
                    temp_flower.GetComponent<RectTransform>().sizeDelta = new Vector2(60.0f, 102.86f);
                    if (yellow)
                        temp_flower.GetComponent<Image>().sprite = flower_sprites[1];
                    else
                        temp_flower.GetComponent<Image>().sprite = flower_sprites[2];

                    round_temp -= 10;
                    yellow = !yellow;
                }

                //now spawn white flowers
                for (int i = 0; i < round_temp; i++)
                    Instantiate(flower_prefab, ui_elements[3].transform);

                //now set the round text
                ui_elements[4].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"ROUND\n{value}";
            }
        }
    }

    public int lives
    {
        get { return _lives; }
        set 
        {
            if (value < 12)
            {
                _lives = value;

                if (ui != null)
                {
                    for (int i = 0; i < ui_elements[2].transform.childCount; i++)
                    {
                        bool enabled = i <= value - 2; 
                        ui_elements[2].transform.GetChild(i).GetComponent<Image>().enabled = enabled;
                    }
                }
            }
        }
    }

    public int alive_enemies
    {
        get { return _alive_enemies; }
        set { _alive_enemies = value; }
    }

    public int rocks_dropped
    {
        get { return _rocks_dropped; }
        set { 
            _rocks_dropped = value;
            if (_rocks_dropped == 2)
                Instantiate(vegetable, Vector3.zero, Quaternion.identity);
        }
    }

    public void ReloadUI()
    {
        if (GameObject.FindGameObjectWithTag("UI") != null)
        {
            ui = GameObject.FindGameObjectWithTag("UI");

            ui_elements = new GameObject[5];

            ui_elements[0] = ui.transform.GetChild(1).gameObject;
            ui_elements[1] = ui.transform.GetChild(2).gameObject;
            ui_elements[2] = ui.transform.GetChild(3).gameObject;
            ui_elements[3] = ui.transform.GetChild(4).gameObject;
            ui_elements[4] = ui.transform.GetChild(5).gameObject;
        }

        score = _score;
        if (!PlayerPrefs.HasKey("Highscore"))
            PlayerPrefs.SetInt("Highscore", 0);

        highscore = PlayerPrefs.GetInt("Highscore");
        level = _level;
        lives = _lives;
        rocks_dropped = 0;
    }

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;

        high_score_broken = false;
        game_over = false;

        first_bonus = false;
        second_bonus = false;

        ReloadUI();

        end_sound = GetComponent<AudioSource>();

        score = 0;

        level = 3;
        lives = 3;

        rocks_dropped = 0;

        _palette_index = 0;

        ground_mat_instance = new Material(ground_mat_instance);

        DontDestroyOnLoad(gameObject);

        StartCoroutine(LastEnemyRoutine());
    }

    private void Start()
    {
        leaderboard_public_key = "ac8601e7913c6dc8376e5b79d95f7d8344d587e2a0d0305dcf3012166f787c46";
        LoadTop5();
    }

    private void Update()
    {
        if(GameObject.FindGameObjectWithTag("Player") != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player.GetComponent<PlayerMovement>().GetDead())
                RespawnPlayer(player);
        }
    }
}
