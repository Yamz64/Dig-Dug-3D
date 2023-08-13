using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionSlider : MonoBehaviour
{
    [SerializeField]
    private int default_value, slider_min, slider_max;

    [SerializeField]
    private string preference_name;

    private TextMeshProUGUI option_value;
    private Slider harpoon_scroll;
    private Image monster;

    [SerializeField]
    private List<Sprite> monster_sprites;

    private AudioSource test_sound_volume;

    // Function updates the display value of the option slider as the scrollbar changes
    void UpdateDisplayValue(float scroll_value)
    {
        option_value.text = ((int)Mathf.Lerp(slider_min, slider_max, scroll_value)).ToString();
    }

    // Function updates the monster sprite whenever the scrollbar changes
    void UpdateMonsterSprite(float scroll_value)
    {
        if(monster_sprites.Count == 0)
        {
            Debug.LogWarning("Cannot set monster sprite because none are specified!");
            return;
        }

        float sprite_threshold = 1.0f / (monster_sprites.Count - 1);

        for(int i=0; i<monster_sprites.Count; i++)
        {
            if(scroll_value < sprite_threshold * (i + 1))
            {
                monster.sprite = monster_sprites[i];
                return;
            }
        }
    }

    //Generic function to add a player preference entry to a string name
    public void AdjustSliderPreference(float value)
    {
        //if there is nothing provided then abort
        if (preference_name == "")
        {
            Debug.LogWarning("No preference name provided, aborting...");
            return;
        }

        int preference_value = (int)Mathf.Lerp(slider_min, slider_max, value);
        PlayerPrefs.SetInt(preference_name, preference_value);
    }

    public void PlaySound(float value)
    {
        test_sound_volume.volume = value;
        if (!test_sound_volume.isPlaying)
            test_sound_volume.Play();
    }

    // Start is called before the first frame update
    void Start()
    {
        option_value = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        harpoon_scroll = transform.GetChild(2).GetComponent<Slider>();
        monster = transform.GetChild(transform.childCount - 1).GetComponent<Image>();
        test_sound_volume = GetComponent<AudioSource>();

        option_value.text = "0";

        //before subscribing to the onvaluechanged function, set this slider to it's player preference
        if (!PlayerPrefs.HasKey(preference_name))
            PlayerPrefs.SetInt(preference_name, default_value);

        Debug.Log($"{preference_name}: {PlayerPrefs.GetInt(preference_name)}");

        harpoon_scroll.value = (float)(PlayerPrefs.GetInt(preference_name) - slider_min) / (slider_max - slider_min);
        UpdateDisplayValue(harpoon_scroll.value);
        UpdateMonsterSprite(harpoon_scroll.value);

        harpoon_scroll.onValueChanged.AddListener(UpdateDisplayValue);
        harpoon_scroll.onValueChanged.AddListener(UpdateMonsterSprite);
        harpoon_scroll.onValueChanged.AddListener(AdjustSliderPreference);

        if (test_sound_volume != null)
            harpoon_scroll.onValueChanged.AddListener(PlaySound);
    }
}
