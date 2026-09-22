// MiniDialogueUI
// A small text popup used for quick messages when the player interacts with
// something: a short "mini dialogue", or a reason an item cannot be taken yet.
// It shows a line of text for a few seconds, then hides itself. It uses real
// (unscaled) time so it still counts down even when the game is frozen.
// The full conversation system (with speaker names and choices) comes later; this
// is only for one short line at a time.
//
// Put this on: a "MiniDialogue" GameObject on your main (persistent) Canvas.
// Assign in Inspector:
//   - Panel: the small popup GameObject to show/hide.
//   - Message Text: the TextMeshPro text inside the popup.
//   - Default Duration: how long the message stays if no time is given.

using System.Collections;
using TMPro;
using UnityEngine;

public class MiniDialogueUI : Singleton<MiniDialogueUI>
{
    [Header("Parts")]
    [Tooltip("The popup GameObject that is shown and hidden.")]
    [SerializeField] private GameObject panel;

    [Tooltip("The TextMeshPro text that shows the message.")]
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [Tooltip("How long a message stays on screen, in seconds, when no other time is given.")]
    [SerializeField] private float defaultDuration = 2.5f;

    private Coroutine hideRoutine;

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // Shows a plain text message for a while (uses Default Duration if seconds <= 0).
    public void Show(string message, float seconds = -1f)
    {
        if (panel == null || messageText == null)
        {
            Debug.LogWarning("MiniDialogueUI: Panel or Message Text is not assigned. Assign them in the Inspector.", this);
            return;
        }

        messageText.text = message;
        panel.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }
        hideRoutine = StartCoroutine(HideAfter(seconds > 0f ? seconds : defaultDuration));
    }

    // Shows a message from a LocalizedString in the player's current language.
    public void ShowLocalized(LocalizedString message, float seconds = -1f)
    {
        string text = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(message)
            : message.english;

        Show(text, seconds);
    }

    // Hides the popup after the given time (real time, so pausing does not block it).
    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        panel.SetActive(false);
        hideRoutine = null;
    }
}
