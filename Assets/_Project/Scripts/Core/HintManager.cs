// HintManager
// Watches how long it has been since the player last interacted with anything.
// If the player is stuck (no interaction for the Idle Hint Seconds), it turns on
// "hint mode", and interactable objects with a HighlightEffect glow to guide the
// player. Any interaction resets the timer.
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector: Idle Hint Seconds (how long before hints appear).

using UnityEngine;

public class HintManager : Singleton<HintManager>
{
    [Header("Hint")]
    [Tooltip("How many seconds without interacting before hint highlights appear.")]
    [SerializeField] private float idleHintSeconds = 8f;

    private float timeSinceLastInteraction;

    // True when the player has been idle long enough to show hints.
    public bool ShouldShowHint => timeSinceLastInteraction >= idleHintSeconds;

    private void Update()
    {
        // Only count idle time during normal play.
        if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
        {
            timeSinceLastInteraction += Time.deltaTime;
        }
    }

    // Call this whenever the player interacts, to reset the hint timer.
    public void NotifyInteraction()
    {
        timeSinceLastInteraction = 0f;
    }
}
