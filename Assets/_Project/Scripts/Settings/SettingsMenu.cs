// SettingsMenu
// The options screen. It has sliders for Music, Sound, and Brightness, and a
// dropdown for Language. When the player changes a control, the change is applied
// right away (so they can hear/see it) and remembered. The settings are written to
// the global settings save when the menu closes.
//
// Put this on: the Settings panel GameObject (under your Canvas).
// Assign in Inspector:
//   - Music Slider / Sound Slider: volume from 0 to 1.
//   - Brightness Slider: its range is set automatically (10 to the maximum).
//   - Language Dropdown: a TMP_Dropdown; its options are filled automatically.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Slider for background music volume (0 to 1).")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("Slider for sound effect volume (0 to 1).")]
    [SerializeField] private Slider soundSlider;

    [Header("Brightness")]
    [Tooltip("Slider for screen brightness. Its range is set from the BrightnessController.")]
    [SerializeField] private Slider brightnessSlider;

    [Header("Language")]
    [Tooltip("Dropdown for choosing the language.")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    // True while we set the controls from saved values, so change events are ignored.
    private bool isInitializing;

    private void Awake()
    {
        SetupControls();
    }

    private void OnEnable()
    {
        RefreshFromSettings();
    }

    private void OnDisable()
    {
        // Save the settings when the menu is closed.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveSettings();
        }
    }

    // Sets slider ranges, fills the dropdown, and connects the change events.
    private void SetupControls()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }
        if (soundSlider != null)
        {
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.onValueChanged.AddListener(OnSoundChanged);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = BrightnessController.SliderMinimum;
            brightnessSlider.maxValue = BrightnessController.Instance != null
                ? BrightnessController.Instance.SliderMaximum
                : 60f;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>(Enum.GetNames(typeof(Language))));
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    // Shows the current saved values on the controls (without triggering saves).
    private void RefreshFromSettings()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        SettingsSaveData settings = SaveManager.Instance.Settings;
        isInitializing = true;

        if (musicSlider != null)
        {
            musicSlider.value = settings.musicVolume;
        }
        if (soundSlider != null)
        {
            soundSlider.value = settings.soundVolume;
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.value = settings.brightness;
        }
        if (languageDropdown != null)
        {
            languageDropdown.value = (int)settings.language;
        }

        isInitializing = false;
    }

    // Applies and remembers the music volume.
    private void OnMusicChanged(float value)
    {
        if (isInitializing)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        SaveManager.Instance.Settings.musicVolume = value;
    }

    // Applies and remembers the sound effect volume.
    private void OnSoundChanged(float value)
    {
        if (isInitializing)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSoundVolume(value);
        }
        SaveManager.Instance.Settings.soundVolume = value;
    }

    // Applies and remembers the brightness.
    private void OnBrightnessChanged(float value)
    {
        if (isInitializing)
        {
            return;
        }

        if (BrightnessController.Instance != null)
        {
            BrightnessController.Instance.ApplyBrightness(value);
        }
        SaveManager.Instance.Settings.brightness = value;
    }

    // Applies and remembers the language.
    private void OnLanguageChanged(int index)
    {
        if (isInitializing)
        {
            return;
        }

        Language language = (Language)index;
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(language);
        }
        SaveManager.Instance.Settings.language = language;
    }
}
