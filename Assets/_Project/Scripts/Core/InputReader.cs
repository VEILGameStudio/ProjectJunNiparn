// InputReader
// One shared place that reads the player's controls (keyboard + gamepad) and
// hands the results to the rest of the game. Movement scripts read MoveInput and
// RunHeld every frame; other scripts poll InteractPressed / ToggleInventoryPressed
// / PausePressed once per frame.
//
// It reads the actions LIVE from the Controls asset every time (no cached copies).
// This avoids a bug where a cached action map could point at a stale copy after a
// domain reload, so input silently stopped working.
//
// This is a ScriptableObject asset, not a component. Create the asset with:
//   Right-click in Project > Create > Game > Input Reader
// Then drag the "GameControls" Input Actions asset into its field, and drag this
// Input Reader asset into any script that needs input (Player, UI, etc.).
//
// Assign in Inspector: "Controls" (the GameControls.inputactions asset).

using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject
{
    [Header("Controls")]
    [Tooltip("Drag the GameControls Input Actions asset here.")]
    [SerializeField] private InputActionAsset controls;

    private const string GameplayMapName = "Gameplay";

    // Live movement values, read straight from the actions each time.
    public Vector2 MoveInput
    {
        get { InputAction action = GetGameplayAction("Move"); return action != null ? action.ReadValue<Vector2>() : Vector2.zero; }
    }

    public bool RunHeld
    {
        get { InputAction action = GetGameplayAction("Run"); return action != null && action.IsPressed(); }
    }

    // One-shot checks: true only on the frame the button goes down. Poll these once
    // per frame from Update (they replace the old OnInteract / OnToggleInventory events).
    public bool InteractPressed => WasPressedThisFrame("Interact");
    public bool ToggleInventoryPressed => WasPressedThisFrame("OpenInventory");
    public bool PausePressed => WasPressedThisFrame("Pause");

    // Turns the gameplay controls on. Call when normal gameplay begins.
    public void EnableGameplay()
    {
        InputActionMap map = GetGameplayMap();
        if (map == null)
        {
            return;
        }

        map.Enable();
    }

    // Turns the gameplay controls off (for example during a cutscene).
    public void DisableGameplay()
    {
        InputActionMap map = GetGameplayMap();
        if (map != null)
        {
            map.Disable();
        }
    }

    // Finds the Gameplay action map live from the Controls asset.
    private InputActionMap GetGameplayMap()
    {
        if (controls == null)
        {
            Debug.LogError($"InputReader '{name}': Controls is not assigned. Drag the GameControls asset into the Controls field.", this);
            return null;
        }
        return controls.FindActionMap(GameplayMapName);
    }

    // Finds one gameplay action live, making sure the map is on so it can be read.
    private InputAction GetGameplayAction(string actionName)
    {
        InputActionMap map = GetGameplayMap();
        if (map == null)
        {
            return null;
        }
        if (!map.enabled)
        {
            map.Enable(); // Self-heal: guarantee the controls are on when we read them.
        }
        return map.FindAction(actionName);
    }

    // True only on the frame the named button is first pressed.
    private bool WasPressedThisFrame(string actionName)
    {
        InputAction action = GetGameplayAction(actionName);
        return action != null && action.WasPressedThisFrame();
    }
}
