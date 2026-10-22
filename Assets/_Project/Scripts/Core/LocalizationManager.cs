// LocalizationManager
// Remembers which language the player chose and turns a LocalizedString into the
// correct text. Any UI or dialogue script asks this manager for its words:
//   GameManager.Instance.Localization.Get(myLocalizedString)
//
// Put this on: the "Managers" GameObject (next to the GameManager).
// Assign in Inspector: "Default Language" (used the very first time the game runs).

using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    [Header("Language")]
    [Tooltip("The language used the first time the game runs, before the player picks one in Settings.")]
    [SerializeField] private Language defaultLanguage = Language.English;

    // The language currently shown in the game.
    public Language CurrentLanguage { get; private set; }

    private void Awake()
    {
        CurrentLanguage = defaultLanguage;
    }

    // Turns a LocalizedString into plain text in the player's current language.
    public string Get(LocalizedString text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        return text.ForLanguage(CurrentLanguage);
    }

    // Changes the current language and tells the rest of the game to refresh.
    // Call this from the Settings menu. It does not save by itself; the Settings
    // menu decides when to save so all settings are written together.
    public void SetLanguage(Language newLanguage)
    {
        if (CurrentLanguage == newLanguage)
        {
            return;
        }

        CurrentLanguage = newLanguage;
        GameEvents.RaiseLanguageChanged();
    }
}
