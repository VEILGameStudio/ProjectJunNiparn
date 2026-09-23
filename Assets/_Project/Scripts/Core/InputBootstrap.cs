// InputBootstrap
// Switches the shared controls on when the scene starts and off when it ends.
// It also runs a quick self-check in the Editor that says exactly what is wrong if
// the input setup is broken, instead of the player just not moving.
//
// Put this on: the "Managers" GameObject in every gameplay scene (one per scene).
// Assign in Inspector: Input Reader = the MainInputReader asset.

using UnityEngine;
using UnityEngine.InputSystem;

public class InputBootstrap : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared MainInputReader asset here.")]
    [SerializeField] private InputReader inputReader;

    private void Awake()
    {
        if (inputReader == null)
        {
            Debug.LogError($"InputBootstrap on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
            return;
        }

        inputReader.Initialize();
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.Shutdown();
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Checks the input setup once the scene is running and reports anything wrong.
    private void Start()
    {
        if (inputReader == null)
        {
            return;
        }

        if (inputReader.ActiveAsset == null)
        {
            Debug.LogError($"InputBootstrap on '{name}': the Input Reader has no Controls asset assigned.", this);
            return;
        }

        if (!inputReader.IsGameplayEnabled)
        {
            Debug.LogError($"InputBootstrap on '{name}': the Gameplay controls are OFF right after startup. Something turned them off - check for a cutscene or a script calling DisableGameplay.", this);
        }

        CheckForConflictingPlayerInput();
    }

    // A PlayerInput component makes its own copy of the actions asset. If one exists
    // and uses a different copy, the game would read a different set of controls.
    private void CheckForConflictingPlayerInput()
    {
        PlayerInput[] playerInputs = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (PlayerInput playerInput in playerInputs)
        {
            if (playerInput.actions != inputReader.ActiveAsset)
            {
                string otherName = playerInput.actions != null ? playerInput.actions.name : "none";
                Debug.LogError($"InputBootstrap: PlayerInput on '{playerInput.name}' uses a different Input Actions asset ('{otherName}') than the Input Reader ('{inputReader.ActiveAsset.name}'). Both must use the same one, or remove the PlayerInput component.", playerInput);
            }
        }
    }
#endif
}
