// InputBootstrap
// Switches the shared controls on when the game starts and off when it ends.
// It also runs a quick self-check in the Editor that says exactly what is wrong if
// the input setup is broken, instead of the player just not moving.
//
// Only ONE InputBootstrap owns the controls: the first one that starts. When a
// scene is reloaded (for example after game over), that scene's copy of the
// Managers object is a duplicate that gets removed. The duplicate does nothing,
// so removing it can never switch the controls off.
//
// Put this on: the "Managers" GameObject (the one with the GameManager).
// Assign in Inspector: Input Reader = the MainInputReader asset.

using UnityEngine;
using UnityEngine.InputSystem;

public class InputBootstrap : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared MainInputReader asset here.")]
    [SerializeField] private InputReader inputReader;

    // The bootstrap that switched the controls on. Only this one may switch them off.
    private static InputBootstrap activeBootstrap;

    // True when this bootstrap owns the controls (it is not a duplicate).
    private bool IsActiveBootstrap => activeBootstrap == this;

    // Switches the controls on, unless another bootstrap already did.
    private void Awake()
    {
        if (activeBootstrap != null && activeBootstrap != this)
        {
            return; // A duplicate from a reloaded scene. The first bootstrap owns the controls.
        }

        if (inputReader == null)
        {
            Debug.LogError($"InputBootstrap on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
            return;
        }

        activeBootstrap = this;
        inputReader.Initialize();
    }

    // Switches the controls off, but only if this bootstrap switched them on.
    private void OnDestroy()
    {
        if (!IsActiveBootstrap)
        {
            return;
        }

        activeBootstrap = null;
        inputReader.Shutdown();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Checks the input setup once the scene is running and reports anything wrong.
    private void Start()
    {
        if (!IsActiveBootstrap)
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
