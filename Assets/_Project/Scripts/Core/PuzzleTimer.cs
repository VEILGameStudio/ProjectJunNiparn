// PuzzleTimer
// A countdown used by certain puzzles or missions. It shows the time left on a UI
// text. If time runs out, it plays a Fail animation (an Animator trigger you name
// in the Inspector) and then runs the On Timer End event. Later the GameOverManager
// will listen to that event; for now wire whatever you like there.
// The timer automatically stops while the game is paused (menu/inventory open),
// because it only counts down during normal play.
//
// Put this on: the puzzle GameObject that needs a timer.
// Assign in Inspector:
//   - Duration Seconds: how long the player has.
//   - Start Automatically: tick to start when the scene loads.
//   - Timer Text (optional): a TextMeshPro text showing the time left.
//   - Fail Animator + Fail Trigger Name (optional): the Fail animation to play.
//   - On Timer End: what happens when time runs out.

using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleTimer : MonoBehaviour, ISaveParticipant
{
    [Header("Timer")]
    [Tooltip("How many seconds the player has.")]
    [SerializeField] private float durationSeconds = 60f;

    [Tooltip("Tick to start counting down as soon as the scene loads.")]
    [SerializeField] private bool startAutomatically = true;

    [Tooltip("Tick to include this timer's value in the save file. Use only one saved timer per scene.")]
    [SerializeField] private bool saveThisTimer = true;

    [Header("UI")]
    [Tooltip("TextMeshPro text that shows the remaining time (mm:ss). Optional.")]
    [SerializeField] private TMP_Text timerText;

    [Header("Fail Animation")]
    [Tooltip("The Animator that plays the fail animation. Optional.")]
    [SerializeField] private Animator failAnimator;

    [Tooltip("The Animator Trigger parameter name to fire on failure, e.g. 'Fail'.")]
    [SerializeField] private string failTriggerName = "Fail";

    [Header("Events")]
    [Tooltip("Runs once when the time runs out. The GameOverManager will use this later.")]
    [SerializeField] private UnityEvent onTimerEnd;

    private float timeRemaining;
    private bool isRunning;
    private bool wasRestored;

    // How much time is left, in seconds.
    public float TimeRemaining => timeRemaining;

    // True while the countdown is active.
    public bool IsRunning => isRunning;

    private void Awake()
    {
        timeRemaining = durationSeconds; // Ready before any autosave reads it.
    }

    private void Start()
    {
        // Register first: if a saved game is loading, this restores our value.
        if (saveThisTimer && SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }

        // Only auto-start when we were not restored from a save.
        if (startAutomatically && !wasRestored)
        {
            StartTimer();
        }
        else
        {
            UpdateDisplay();
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    private void Update()
    {
        // Only count down during normal play, so menus/inventory/dialogue stop it.
        if (!isRunning || !IsGamePlaying())
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateDisplay();
            TimeUp();
            return;
        }

        UpdateDisplay();
    }

    // Starts (or restarts) the countdown from the full duration.
    public void StartTimer()
    {
        timeRemaining = durationSeconds;
        isRunning = true;
        UpdateDisplay();
    }

    // Stops the countdown without failing (for example when the puzzle is solved).
    public void StopTimer()
    {
        isRunning = false;
    }

    // Adds (or removes, with a negative number) seconds from the remaining time.
    public void AddTime(float seconds)
    {
        timeRemaining = Mathf.Max(0f, timeRemaining + seconds);
        UpdateDisplay();
    }

    // Runs when the timer reaches zero: play the fail animation, then the event.
    private void TimeUp()
    {
        isRunning = false;

        if (failAnimator != null && !string.IsNullOrEmpty(failTriggerName))
        {
            failAnimator.SetTrigger(failTriggerName);
        }

        onTimerEnd?.Invoke();

        // Hand off to the shared game-over sequence (fade, wait, reload last save).
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver();
        }
    }

    // True only during normal gameplay.
    private bool IsGamePlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // --- Saving (ISaveParticipant) ---

    // Writes this timer's value into the save.
    public void CaptureState(GameSaveData data)
    {
        data.hasTimer = true;
        data.timerRemaining = timeRemaining;
        data.timerRunning = isRunning;
    }

    // Restores this timer's value from the save (if one was saved).
    public void RestoreState(GameSaveData data)
    {
        if (!data.hasTimer)
        {
            return;
        }

        timeRemaining = data.timerRemaining;
        isRunning = data.timerRunning;
        wasRestored = true;
        UpdateDisplay();
    }

    // Shows the remaining time as mm:ss.
    private void UpdateDisplay()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
