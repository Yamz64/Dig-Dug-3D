using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenusFunctionality : MonoBehaviour
{
    [SerializeField]
    private AudioMixer master_mixer;

    //Generic Function for loading 
    public void LoadScene(string scene)
    {
        //first check to see if the scene is in the list of scenes in build
        bool found_scene = false;
        for(int i = 0; i<SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            int last_slash = path.LastIndexOf('/');
            string indexed_scene_name = path.Substring(last_slash + 1, path.LastIndexOf('.') - last_slash - 1);

            if(scene == indexed_scene_name)
            {
                found_scene = true;
                break;
            }
        }

        if (!found_scene)
        {
            Debug.LogWarning($"Scene: {scene}, not in build settings, aborting...");
            return;
        }

        SceneManager.LoadScene(scene);
    }

    //Function for changing the window resolution
    public void ChangeResolution(TMP_Dropdown dropdown)
    {
        string resolution_string = dropdown.options[dropdown.value].text;

        int width = int.Parse(resolution_string.Split('x')[0]);
        int height = int.Parse(resolution_string.Split('x')[1]);

        Screen.SetResolution(width, height, Screen.fullScreen);
    }

    //Function for changing to windowed mode
    public void SetWindowed(Toggle toggle)
    {
        Screen.SetResolution(Screen.width, Screen.height, toggle.isOn);
    }

    //Function quits the game
    public void Quit()
    {
        Application.Quit();
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (master_mixer != null)
        {
            if (PlayerPrefs.HasKey("SFXVolume"))
            {
                float volume_factor = PlayerPrefs.GetInt("SFXVolume") / 100.0f;
                master_mixer.SetFloat("SFXVolume", Mathf.Lerp(-80.0f, 0.0f, volume_factor));
            }
            if (PlayerPrefs.HasKey("MusicVolume"))
            {
                float volume_factor = PlayerPrefs.GetInt("MusicVolume") / 100.0f;
                master_mixer.SetFloat("MusicVolume", Mathf.Lerp(-80.0f, 0.0f, volume_factor));
            }
        }

        if (GameObject.FindGameObjectWithTag("MenuSoundSuppressor"))
            Destroy(GameObject.FindGameObjectWithTag("MenuSoundSuppressor"));
        else
        {
            AudioSource source = GetComponent<AudioSource>();
            GetComponent<AudioSource>().Play();
        }
    }
}
