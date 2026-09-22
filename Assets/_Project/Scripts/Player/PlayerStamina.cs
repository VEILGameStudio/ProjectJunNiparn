// PlayerStamina
// Handles running stamina (0 to 100). Stamina drains only while the player is
// actually running (holding Run AND moving). It does NOT recover while running,
// but recovers while walking or standing still. When stamina hits 0 the player is
// forced to walk and cannot run again until stamina climbs back to the "Can Run
// Again" value (30 by default). When it empties, it flashes/shakes the bar and
// plays a tired sound.
//
// The movement scripts ask this component "CanRun" to decide walk vs run speed.
//
// Put this on: the Player GameObject.
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Max Stamina / Drain / Regen / Can Run Again: the numbers to tune.
//   - Stamina Bar (optional): the UIStatBar that shows stamina.
//   - Tired Sound (optional): a sound played when stamina runs out.

using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Values")]
    [Tooltip("The highest and starting stamina.")]
    [SerializeField] private float maxStamina = 100f;

    [Tooltip("How much stamina is used per second while running.")]
    [SerializeField] private float drainPerSecond = 25f;

    [Tooltip("How much stamina recovers per second while walking or idle.")]
    [SerializeField] private float regenPerSecond = 15f;

    [Tooltip("After hitting 0, stamina must reach this value before the player can run again.")]
    [SerializeField] private float canRunAgainThreshold = 30f;

    [Header("UI")]
    [Tooltip("The bar that shows current stamina. Optional.")]
    [SerializeField] private UIStatBar staminaBar;

    [Header("Feedback")]
    [Tooltip("Sound played once when stamina runs out.")]
    [SerializeField] private AudioClip tiredSound;

    private float currentStamina;

    // True after stamina hits 0, until it recovers to the Can Run Again value.
    private bool isExhausted;

    // How far the stick/keys must move before we count the player as "moving".
    private const float MoveDeadzone = 0.1f;

    // Movement scripts read this to decide whether to run or walk.
    public bool CanRun => !isExhausted && currentStamina > 0f;

    // Current stamina from 0 to 1, handy for other UI.
    public float NormalizedStamina => currentStamina / maxStamina;

    private void Awake()
    {
        currentStamina = maxStamina;

        if (inputReader == null)
        {
            Debug.LogError($"PlayerStamina on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
        }
    }

    private void Start()
    {
        UpdateBar();
    }

    private void Update()
    {
        if (inputReader == null)
        {
            return;
        }

        if (IsRunningNow())
        {
            Drain();
        }
        else
        {
            Regenerate();
        }

        UpdateBar();
    }

    // The player is running only when they want to run, are moving, and are allowed.
    private bool IsRunningNow()
    {
        bool moving = inputReader.MoveInput.sqrMagnitude > MoveDeadzone * MoveDeadzone;
        return inputReader.RunHeld && moving && CanRun;
    }

    // Uses up stamina. Becomes exhausted when it reaches 0.
    private void Drain()
    {
        currentStamina -= drainPerSecond * Time.deltaTime;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            BecomeExhausted();
        }
    }

    // Recovers stamina. Allows running again once past the threshold.
    private void Regenerate()
    {
        if (currentStamina >= maxStamina)
        {
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);

        if (isExhausted && currentStamina >= canRunAgainThreshold)
        {
            isExhausted = false;
        }
    }

    // Runs once when stamina empties: locks running and plays the feedback.
    private void BecomeExhausted()
    {
        if (isExhausted)
        {
            return;
        }

        isExhausted = true;

        if (staminaBar != null)
        {
            staminaBar.PlayWarningEffect();
        }
        if (tiredSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(tiredSound);
        }
    }

    // Updates the stamina bar to match the current stamina.
    private void UpdateBar()
    {
        if (staminaBar != null)
        {
            staminaBar.SetFill(NormalizedStamina);
        }
    }
}
