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
// It autosaves by listening to GameEvents: when a new scene is ready
// (OnSceneReady) and when the player picks up an item (OnItemPickedUp).
// For "level complete", call SaveGame() (for example through SaveActions).
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector: Autosave On Scene Load (ticked by default).

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : Singleton<SaveManager>
{
    [Header("Autosave")]
    [Tooltip("If ticked, the game autosaves each time a new scene finishes loading.")]
    [SerializeField] private bool autosaveOnSceneLoad = true;

    // File names inside Application.persistentDataPath.
    private const string GameSaveFileName = "gamesave.json";
    private const string SettingsFileName = "settings.json";

    // Systems that contribute their data to the autosave.
    private readonly List<ISaveParticipant> participants = new List<ISaveParticipant>();

    // The current settings, kept in memory so any script can read them.
    public SettingsSaveData Settings { get; private set; } = new SettingsSaveData();

    // True if an autosave file already exists on disk.
    public bool HasSave => File.Exists(GameSavePath);

    // True while a saved game is being loaded and restored. Other systems check
    // this so they do not autosave over the file mid-load.
    public bool IsRestoring { get; private set; }

    // The save being restored right now, and whether a restore is in progress.
    private GameSaveData loadingData;
    private bool restorePending;

    private string GameSavePath => Path.Combine(Application.persistentDataPath, GameSaveFileName);
    private string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

    protected override void Awake()
    {
        base.Awake();
        ReadSettingsFile(); // Settings are ready for other scripts from the very start.
    }

    private void Start()
    {
        // Applied in Start, so the GameManager and its systems are ready first.
        ApplySettings();
    }

    private void OnEnable()
    {
        GameEvents.OnSceneReady += HandleSceneReady;
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnSceneReady -= HandleSceneReady;
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
    }

    // A system asks to be included in the autosave. If a load is in progress, the
    // system is restored right away (this handles per-scene objects that appear
    // after the saved scene has loaded).
    public void Register(ISaveParticipant participant)
    {
        if (participant == null || participants.Contains(participant))
        {
            return;
        }

        participants.Add(participant);

        if (restorePending && loadingData != null)
        {
            participant.RestoreState(loadingData);
        }
    }

    // A system no longer wants to be included (call this when it is destroyed).
    public void Unregister(ISaveParticipant participant)
    {
        participants.Remove(participant);
    }

    // Collects data from every registered system and writes the autosave file.
    // Call this to autosave (for example on level complete).
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.sceneName = SceneManager.GetActiveScene().name;

        foreach (ISaveParticipant participant in participants)
        {
            participant.CaptureState(data);
        }

        WriteJson(GameSavePath, data);
    }

    // Loads the saved game: reads the file, loads the saved scene, and restores all
    // systems (inventory, player position, timer). Use this for a "Continue" button
    // or to reload after game over. Returns false if there is no save.
    public bool ContinueGame()
    {
        if (!HasSave)
        {
            Debug.LogWarning("SaveManager: ContinueGame was called but no save file exists yet.");
            return false;
        }

        loadingData = ReadJson<GameSaveData>(GameSavePath);
        if (loadingData == null)
        {
            return false;
        }

        restorePending = true;
        IsRestoring = true;

        // Restore systems that already exist (like the inventory, which lives on the
        // Managers object). Per-scene systems are restored as they register after load.
        foreach (ISaveParticipant participant in participants)
        {
            participant.RestoreState(loadingData);
        }

        if (GameManager.Instance != null && GameManager.Instance.SceneLoader != null)
        {
            GameManager.Instance.SceneLoader.LoadScene(loadingData.sceneName, null);
        }
        else
        {
            Debug.LogError("SaveManager: no SceneLoader found, so the saved scene cannot load. Make sure the Managers object has a SceneLoader.", this);
            ClearRestore();
        }
        return true;
    }

    // A new scene is ready: finish a Continue that is in progress, or autosave.
    private void HandleSceneReady()
    {
        if (restorePending)
        {
            StartCoroutine(FinishRestore());
            return;
        }

        if (autosaveOnSceneLoad)
        {
            SaveGame();
        }
    }

    // Autosaves right after the player picks something up.
    private void HandleItemPickedUp(ItemData item, int amount)
    {
        SaveGame();
    }

    // Gives the loaded scene's new objects a couple of frames to register (and be
    // restored), then finishes the restore.
    private IEnumerator FinishRestore()
    {
        yield return null; // Let new objects' Start run and register.
        yield return null;
        ClearRestore();
    }

    private void ClearRestore()
    {
        restorePending = false;
        IsRestoring = false;
        loadingData = null;
    }

    // Writes the current settings to the settings file.
    public void SaveSettings()
    {
        WriteJson(SettingsPath, Settings);
    }

    // Reads the settings file (or uses defaults the first time) and applies them.
    public void LoadSettings()
    {
        ReadSettingsFile();
        ApplySettings();
    }

    // Reads the settings file into Settings. Keeps the defaults if there is no file yet.
    private void ReadSettingsFile()
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        SettingsSaveData loaded = ReadJson<SettingsSaveData>(SettingsPath);
        if (loaded != null)
        {
            Settings = loaded;
        }
    }

    // Pushes the current settings into the systems that use them.
    private void ApplySettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(Settings.musicVolume);
            AudioManager.Instance.SetSoundVolume(Settings.soundVolume);
        }

        if (GameManager.Instance == null)
        {
            return;
        }
        if (GameManager.Instance.Localization != null)
        {
            GameManager.Instance.Localization.SetLanguage(Settings.language);
        }
        if (GameManager.Instance.Brightness != null)
        {
            GameManager.Instance.Brightness.ApplyBrightness(Settings.brightness);
        }
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
