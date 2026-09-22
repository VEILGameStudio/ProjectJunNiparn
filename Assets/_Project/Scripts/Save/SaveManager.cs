// SaveManager
// Saves and loads the game. There are two files:
//   - gamesave.json  : the autosave (inventory, level, player position, time).
//   - settings.json  : the global settings (music, sound, brightness, language).
// Both files are stored in Application.persistentDataPath (a safe folder Unity
// gives every game on the player's computer).
//
// Systems that want their data saved implement ISaveParticipant and call
// Register(this) / Unregister(this). The SaveManager then asks each of them to
// fill in the save when SaveGame() runs.
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector: nothing required.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    // File names inside Application.persistentDataPath.
    private const string GameSaveFileName = "gamesave.json";
    private const string SettingsFileName = "settings.json";

    // Systems that contribute their data to the autosave.
    private readonly List<ISaveParticipant> participants = new List<ISaveParticipant>();

    // The current settings, kept in memory so any script can read them.
    public SettingsSaveData Settings { get; private set; } = new SettingsSaveData();

    // True if an autosave file already exists on disk.
    public bool HasSave => File.Exists(GameSavePath);

    private string GameSavePath => Path.Combine(Application.persistentDataPath, GameSaveFileName);
    private string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

    protected override void Awake()
    {
        base.Awake();
        LoadSettings(); // Settings should always be ready as soon as the game starts.
    }

    // A system asks to be included in the autosave.
    public void Register(ISaveParticipant participant)
    {
        if (participant != null && !participants.Contains(participant))
        {
            participants.Add(participant);
        }
    }

    // A system no longer wants to be included (call this when it is destroyed).
    public void Unregister(ISaveParticipant participant)
    {
        participants.Remove(participant);
    }

    // Collects data from every registered system and writes the autosave file.
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();

        foreach (ISaveParticipant participant in participants)
        {
            participant.CaptureState(data);
        }

        WriteJson(GameSavePath, data);
    }

    // Reads the autosave file and gives the data back to every registered system.
    // Returns false if there was no save to load.
    public bool LoadGame()
    {
        if (!HasSave)
        {
            Debug.LogWarning("SaveManager: LoadGame was called but no save file exists yet.");
            return false;
        }

        GameSaveData data = ReadJson<GameSaveData>(GameSavePath);
        if (data == null)
        {
            return false;
        }

        foreach (ISaveParticipant participant in participants)
        {
            participant.RestoreState(data);
        }
        return true;
    }

    // Writes the current settings to the settings file.
    public void SaveSettings()
    {
        WriteJson(SettingsPath, Settings);
    }

    // Reads the settings file (or uses defaults the first time) and applies them.
    public void LoadSettings()
    {
        if (File.Exists(SettingsPath))
        {
            SettingsSaveData loaded = ReadJson<SettingsSaveData>(SettingsPath);
            if (loaded != null)
            {
                Settings = loaded;
            }
        }

        ApplySettings();
    }

    // Pushes the current settings into the systems that use them.
    private void ApplySettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(Settings.musicVolume);
            AudioManager.Instance.SetSoundVolume(Settings.soundVolume);
        }
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(Settings.language);
        }
        // Brightness is applied by the BrightnessController (added in a later milestone).
    }

    // Turns any data object into JSON text and writes it to a file.
    private void WriteJson(string path, object data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
        }
        catch (IOException error)
        {
            Debug.LogError($"SaveManager: could not write '{path}'. {error.Message}");
        }
    }

    // Reads JSON text from a file and turns it back into a data object.
    private T ReadJson<T>(string path) where T : class
    {
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        catch (IOException error)
        {
            Debug.LogError($"SaveManager: could not read '{path}'. {error.Message}");
            return null;
        }
    }
}
