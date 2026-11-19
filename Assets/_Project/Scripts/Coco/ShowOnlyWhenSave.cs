// ShowOnlyWhenSave
// Shows a button (or any object) only when a saved game exists. Use it for the
// "Continue" button: before the player has played, there is no save, so it is hidden.
//
// Put this on: the Main Menu panel (an object that stays active), NOT on the button itself,
//   because a switched-off object cannot run its own code.
// Assign in Inspector: Target Object = the Continue button.

using UnityEngine;

public class ShowOnlyWhenSave : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object to show only when a save exists. Drag the Continue button here.")]
    [SerializeField] private GameObject targetObject;

    private void OnEnable()
    {
        Refresh();
    }

    // Switches the target on if a save file exists, off if not. Can also be called from a UnityEvent.
    public void Refresh()
    {
        if (targetObject == null)
        {
            Debug.LogError($"ShowOnlyWhenSave on '{name}': Target Object is not assigned. Drag the Continue button here.", this);
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning($"ShowOnlyWhenSave on '{name}': no SaveManager found in this scene, so the Continue button stays hidden. Add the Managers object.", this);
        }

        bool hasSave =SaveManager.Instance != null && SaveManager.Instance.HasSave;
        targetObject.SetActive(hasSave);
    }
}
