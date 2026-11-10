// SaveActions
// A small helper so buttons and events can use the SaveManager without needing a
// direct reference to it (the SaveManager lives on a manager object that is not in
// every scene). Drop this on any object and point a UI Button's OnClick or a
// UnityEvent (like "level complete") at these methods.
//
// Put this on: any GameObject in a scene (for example a Main Menu object, or a
//   level-complete trigger).
// Assign in Inspector: nothing required. Wire buttons/events to the public methods.

using UnityEngine;

public class SaveActions : MonoBehaviour
{
    [Tooltip("The scene to load when New Game is pressed.")]
    [SerializeField] private string newGameSceneName;

    // True if a saved game exists. Use this to show/hide a Continue button.
    public bool HasSave => SaveManager.Instance != null && SaveManager.Instance.HasSave;

    // Autosaves now. Wire this to a "level complete" event or a save point.
    public void SaveNow()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    // Loads the last save. Wire this to a "Continue" button.
    public void Continue()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueGame();
        }
    }

    // Starts a new game by loading the chosen scene. Wire this to a "New Game" button.
    public void NewGame()
    {
        if (string.IsNullOrEmpty(newGameSceneName))
        {
            Debug.LogError($"SaveActions on '{name}': New Game Scene Name is empty. Type the first scene's name.", this);
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.SceneLoader == null)
        {
            Debug.LogError($"SaveActions on '{name}': no SceneLoader found. Make sure the Managers object (with a SceneLoader) is in the scene.", this);
            return;
        }

        GameManager.Instance.SceneLoader.LoadScene(newGameSceneName, null);
    }
}
