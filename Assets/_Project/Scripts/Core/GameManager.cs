// GameManager
// The "brain" of the game. It remembers the current game state (Playing, Paused,
// Cutscene, GameOver) and is the single place other scripts ask "can the player
// move right now?". It locks player input while a dialogue is open.
//
// It is also the one place to reach the other game systems on the Managers object:
//   GameManager.Instance.Inventory.Add(item);
//   GameManager.Instance.SceneLoader.LoadScene("Level2", "FromLevel1");
// The Managers object survives scene changes, so the inventory and game state
// persist from level to level.
//
// Put this on: the "Managers" GameObject.
// Assign in Inspector: the Systems fields. Leave them empty when those components
//   are on this same Managers object - they are found automatically.

using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Systems (found automatically if on this object)")]
    [Tooltip("The Inventory that holds the player's items.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("The SceneLoader that doors use to change scenes.")]
    [SerializeField] private SceneLoader sceneLoader;

    [Tooltip("The PauseManager that freezes time while a menu or the inventory is open.")]
    [SerializeField] private PauseManager pauseManager;

    [Tooltip("The LocalizationManager that turns text into the chosen language.")]
    [SerializeField] private LocalizationManager localization;

    [Tooltip("The HintManager that counts how long the player has been idle.")]
    [SerializeField] private HintManager hintManager;

    [Tooltip("The BrightnessController that changes screen brightness.")]
    [SerializeField] private BrightnessController brightness;

    [Header("Read Only (for checking in the editor)")]
    [Tooltip("The current state of the game. Set automatically at runtime.")]
    [SerializeField] private GameState currentState = GameState.Playing;

    // The systems other scripts can reach through GameManager.Instance.
    public Inventory Inventory => inventory;
    public SceneLoader SceneLoader => sceneLoader;
    public PauseManager PauseManager => pauseManager;
    public LocalizationManager Localization => localization;
    public HintManager HintManager => hintManager;
    public BrightnessController Brightness => brightness;

    // The current game state. Other scripts read this to decide what to do.
    public GameState CurrentState => currentState;

    // True only during normal gameplay. Player movement scripts check this.
    public bool IsPlaying => currentState == GameState.Playing;

    protected override void Awake()
    {
        base.Awake();
        FindMissingSystems();
    }

    private void OnEnable()
    {
        GameEvents.OnDialogueStarted += HandleDialogueStarted;
        GameEvents.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnDialogueStarted -= HandleDialogueStarted;
        GameEvents.OnDialogueEnded -= HandleDialogueEnded;
    }

    // Changes the game state.
    public void SetState(GameState newState)
    {
        currentState = newState;
    }

    // Locks player input while a dialogue is open.
    private void HandleDialogueStarted()
    {
        SetState(GameState.Cutscene);
    }

    // Releases player input when the dialogue closes, unless something else changed the state.
    private void HandleDialogueEnded()
    {
        if (currentState == GameState.Cutscene)
        {
            SetState(GameState.Playing);
        }
    }

    // Fills empty Systems fields from this object, then reports anything still missing.
    private void FindMissingSystems()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (sceneLoader == null) sceneLoader = GetComponent<SceneLoader>();
        if (pauseManager == null) pauseManager = GetComponent<PauseManager>();
        if (localization == null) localization = GetComponent<LocalizationManager>();
        if (hintManager == null) hintManager = GetComponent<HintManager>();
        if (brightness == null) brightness = GetComponent<BrightnessController>();

        ReportIfMissing(inventory, "Inventory");
        ReportIfMissing(sceneLoader, "SceneLoader");
        ReportIfMissing(pauseManager, "PauseManager");
        ReportIfMissing(localization, "LocalizationManager");
        ReportIfMissing(hintManager, "HintManager");
        ReportIfMissing(brightness, "BrightnessController");
    }

    // Logs a clear error if a system could not be found.
    private void ReportIfMissing(Object system, string componentName)
    {
        if (system == null)
        {
            Debug.LogError($"GameManager on '{name}': no {componentName} found. Add a {componentName} component to the Managers object, or drag one into the Systems field.", this);
        }
    }
}
