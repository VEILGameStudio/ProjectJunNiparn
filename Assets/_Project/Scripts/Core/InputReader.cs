// InputReader
// One shared place that reads the player's controls (keyboard + gamepad) and
// hands the results to the rest of the game. Movement scripts read MoveInput and
// RunHeld every frame; other scripts listen to the events (Interact, etc.).
// Using one asset means every script gets the same controls and Intern B only
// wires the input in a single place.
//
// This is a ScriptableObject asset, not a component. Create the asset with:
//   Right-click in Project > Create > Game > Input Reader
// Then drag the "GameControls" Input Actions asset into its field, and drag this
// Input Reader asset into any script that needs input (Player, UI, etc.).
//
// Assign in Inspector: "Controls" (the GameControls.inputactions asset).

using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject
{
    [Header("Controls")]
    [Tooltip("Drag the GameControls Input Actions asset here.")]
    [SerializeField] private InputActionAsset controls;

    // Live values that movement scripts read every frame.
    public Vector2 MoveInput { get; private set; }
    public bool RunHeld { get; private set; }

    // One-shot events other scripts listen to. Remember to subscribe in OnEnable
    // and unsubscribe in OnDisable.
    public event Action OnInteract;
    public event Action OnToggleInventory;
    public event Action OnPause;

    private InputActionMap gameplayMap;
    private InputAction moveAction;
    private InputAction runAction;
    private InputAction interactAction;
    private InputAction inventoryAction;
    private InputAction pauseAction;

    // True while gameplay controls are turned on. Stops us subscribing twice.
    private bool isGameplayEnabled;

    // Finds all the actions inside the assigned Controls asset. Call this once
    // before using the reader (EnableGameplay does it automatically).
    private void CacheActions()
    {
        if (controls == null)
        {
            Debug.LogError($"InputReader '{name}': Controls is not assigned. Drag the GameControls asset into the Controls field.", this);
            return;
        }

        if (gameplayMap != null)
        {
            return; // Already cached.
        }

        gameplayMap = controls.FindActionMap("Gameplay", throwIfNotFound: true);
        moveAction = gameplayMap.FindAction("Move", throwIfNotFound: true);
        runAction = gameplayMap.FindAction("Run", throwIfNotFound: true);
        interactAction = gameplayMap.FindAction("Interact", throwIfNotFound: true);
        inventoryAction = gameplayMap.FindAction("OpenInventory", throwIfNotFound: true);
        pauseAction = gameplayMap.FindAction("Pause", throwIfNotFound: true);
    }

    // Turns on gameplay controls and starts listening for input.
    // Call this when normal gameplay begins (for example from the player script).
    public void EnableGameplay()
    {
        if (isGameplayEnabled)
        {
            return; // Already on; do not subscribe a second time.
        }

        CacheActions();
        if (gameplayMap == null)
        {
            return;
        }

        moveAction.performed += HandleMove;
        moveAction.canceled += HandleMove;
        runAction.performed += HandleRun;
        runAction.canceled += HandleRun;
        interactAction.performed += HandleInteract;
        inventoryAction.performed += HandleInventory;
        pauseAction.performed += HandlePause;

        gameplayMap.Enable();
        isGameplayEnabled = true;
    }

    // Turns off gameplay controls (for example during a cutscene).
    public void DisableGameplay()
    {
        if (gameplayMap == null || !isGameplayEnabled)
        {
            return;
        }

        moveAction.performed -= HandleMove;
        moveAction.canceled -= HandleMove;
        runAction.performed -= HandleRun;
        runAction.canceled -= HandleRun;
        interactAction.performed -= HandleInteract;
        inventoryAction.performed -= HandleInventory;
        pauseAction.performed -= HandlePause;

        gameplayMap.Disable();
        MoveInput = Vector2.zero;
        RunHeld = false;
        isGameplayEnabled = false;
    }

    private void HandleMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    private void HandleRun(InputAction.CallbackContext context) => RunHeld = context.ReadValueAsButton();
    private void HandleInteract(InputAction.CallbackContext context) => OnInteract?.Invoke();
    private void HandleInventory(InputAction.CallbackContext context) => OnToggleInventory?.Invoke();
    private void HandlePause(InputAction.CallbackContext context) => OnPause?.Invoke();
}
