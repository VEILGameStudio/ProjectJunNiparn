// InputReader
// One shared place that reads the player's controls (keyboard + gamepad) and hands
// the results to the rest of the game. Movement scripts read MoveInput and RunHeld
// every frame; other scripts poll InteractPressed / ToggleInventoryPressed /
// PausePressed once per frame.
//
// It looks the actions up LIVE from the Controls asset every time and never keeps a
// copy. Keeping a copy used to break the game: when Unity reloaded scripts or
// re-imported the controls asset, the copy pointed at a dead object, so input
// silently stopped working.
//
// Lifecycle: InputBootstrap calls Initialize() when the game starts and Shutdown()
// when it ends. If the controls are read while they are turned off, this warns once
// instead of quietly switching them back on, so mistakes are easy to see.
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

    // The controls asset currently in use. Set by Initialize; falls back to the
    // Inspector field so things still work if Initialize was never called.
    private InputActionAsset activeAsset;

    // Stops the "controls are off" warning from repeating every frame.
    private bool hasWarnedControlsOff;

    // Live values, looked up fresh each time they are read.
    public Vector2 MoveInput
    {
        get { InputAction action = GetGameplayAction("Move"); return action != null ? action.ReadValue<Vector2>() : Vector2.zero; }
    }

    public bool RunHeld
    {
        get { InputAction action = GetGameplayAction("Run"); return action != null && action.IsPressed(); }
    }

    // One-shot checks: true only on the frame the button goes down. Poll these once
    // per frame from Update.
    public bool InteractPressed => WasPressedThisFrame("Interact");
    public bool ToggleInventoryPressed => WasPressedThisFrame("OpenInventory");
    public bool PausePressed => WasPressedThisFrame("Pause");

    // The controls asset in use. Used by the startup self-check.
    public InputActionAsset ActiveAsset => activeAsset != null ? activeAsset : controls;

    // True when the gameplay controls are currently switched on.
    public bool IsGameplayEnabled
    {
        get { InputActionMap map = FindGameplayMap(); return map != null && map.enabled; }
    }

    // Gets the controls ready and turns them on. InputBootstrap calls this at start.
    // Safe to call more than once. Pass a different asset only for tests.
    public void Initialize(InputActionAsset source = null)
    {
        activeAsset = source != null ? source : controls;
        if (activeAsset == null)
        {
            Debug.LogError($"InputReader '{name}': Controls is not assigned. Drag the GameControls asset into the Controls field.", this);
            return;
        }

        // throwIfNotFound gives a clear error if the map was renamed in the asset.
        InputActionMap map = activeAsset.FindActionMap(GameplayMapName, throwIfNotFound: true);
        map.Enable();
        hasWarnedControlsOff = false;

        InputDebug.Log($"{name}: Initialize -> '{GameplayMapName}' enabled = {map.enabled}", this);
    }

    // Turns the controls off and forgets the asset. InputBootstrap calls this on exit.
    public void Shutdown()
    {
        InputActionMap map = FindGameplayMap();
        if (map != null)
        {
            map.Disable();
        }

        InputDebug.Log($"{name}: Shutdown", this);
        activeAsset = null;
        hasWarnedControlsOff = false;
    }

    // Turns the gameplay controls on (for example when a cutscene ends).
    public void EnableGameplay()
    {
        InputActionMap map = FindGameplayMap();
        if (map == null)
        {
            return;
        }

        map.Enable();
        hasWarnedControlsOff = false;
        InputDebug.Log($"{name}: EnableGameplay -> enabled = {map.enabled}", this);
    }

    // Turns the gameplay controls off (for example during a cutscene).
    public void DisableGameplay()
    {
        InputActionMap map = FindGameplayMap();
        if (map == null)
        {
            return;
        }

        map.Disable();
        InputDebug.Log($"{name}: DisableGameplay", this);
    }

    // Looks up the Gameplay map live from the asset. Never stores the result.
    private InputActionMap FindGameplayMap()
    {
        InputActionAsset asset = ActiveAsset;
        if (asset == null)
        {
            Debug.LogError($"InputReader '{name}': Controls is not assigned. Drag the GameControls asset into the Controls field.", this);
            return null;
        }
        return asset.FindActionMap(GameplayMapName);
    }

    // Looks up one action live. If the controls are off it warns once and returns
    // nothing, so the mistake shows up instead of being hidden.
    private InputAction GetGameplayAction(string actionName)
    {
        InputActionMap map = FindGameplayMap();
        if (map == null)
        {
            return null;
        }

        if (!map.enabled)
        {
            WarnControlsOff();
            return null;
        }
        return map.FindAction(actionName);
    }

    // Prints the "controls are off" warning only the first time.
    private void WarnControlsOff()
    {
        if (hasWarnedControlsOff)
        {
            return;
        }

        hasWarnedControlsOff = true;
        Debug.LogWarning($"InputReader '{name}': something tried to read input while the '{GameplayMapName}' controls are turned OFF. Make sure an InputBootstrap is in the scene, or call EnableGameplay().", this);
    }

    // True only on the frame the named button is first pressed.
    private bool WasPressedThisFrame(string actionName)
    {
        InputAction action = GetGameplayAction(actionName);
        return action != null && action.WasPressedThisFrame();
    }
}
