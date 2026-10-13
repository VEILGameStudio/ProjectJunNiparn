// SaveData
// Plain data containers that describe what gets written to the save files.
// These are turned into JSON text by the SaveManager. They hold only simple
// values (numbers, text, lists) so JsonUtility can save and load them.
//
// Put this on: nothing. These are data classes used by the SaveManager.

using System.Collections.Generic;

// Everything saved during gameplay (the autosave file).
[System.Serializable]
public class GameSaveData
{
    public string sceneName;            // Which scene the player was in.
    public float playerPositionX;       // Player position, split into x/y/z so
    public float playerPositionY;       // JsonUtility can store it simply.
    public float playerPositionZ;       // (Save files made before the 3D move load this as 0.)
    public List<InventoryEntry> inventoryEntries = new List<InventoryEntry>();

    public bool hasTimer;               // True if a puzzle timer was saved.
    public float timerRemaining;        // Seconds left on the puzzle timer.
    public bool timerRunning;           // Whether that timer was counting down.

    // Things that can be used only once and already were: a key that was picked up, a chest
    // that was opened. Each entry is "<scene name>/<Unique Id>".
    public List<string> usedOneShotIds = new List<string>();
}

// One line of the inventory: which item, and how many.
[System.Serializable]
public class InventoryEntry
{
    public string itemId;
    public int count;
}

// Everything saved in the global settings file (kept even when there is no game).
[System.Serializable]
public class SettingsSaveData
{
    public float musicVolume = 1f;      // 0 = silent, 1 = full.
    public float soundVolume = 1f;      // 0 = silent, 1 = full.
    public float brightness = 10f;      // Slider value; 10 = normal (default).
    public Language language = Language.English;

    // The ids of the secret items the player has found. They are kept here, not in the
    // autosave, so starting a new game does not wipe the player's collection.
    public List<string> foundSecretIds = new List<string>();
}
