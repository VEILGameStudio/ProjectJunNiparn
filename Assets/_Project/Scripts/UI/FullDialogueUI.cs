// FullDialogueUI
// Shows a conversation one line at a time: the speaker's name and what they say.
// The player clicks Continue (or the panel) to move to the next line. When the
// last line is dismissed, it hides itself and tells the DialogueManager it is done.
// The DialogueManager drives this panel - do not call it directly from gameplay.
//
// Put this on: a "FullDialogueUI" panel under your main Canvas.
// Assign in Inspector:
//   - Panel: the window GameObject to show/hide.
//   - Speaker Text / Body Text: TextMeshPro texts for the name and the line.
//   - Continue Button: the button that shows the next line.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FullDialogueUI : MonoBehaviour
{
    [Header("Window")]
    [Tooltip("The conversation window that is shown or hidden.")]
    [SerializeField] private GameObject panel;

    [Header("Text")]
    [Tooltip("TextMeshPro text that shows who is speaking.")]
    [SerializeField] private TMP_Text speakerText;

    [Tooltip("TextMeshPro text that shows the current line.")]
    [SerializeField] private TMP_Text bodyText;

    [Header("Buttons")]
    [Tooltip("The button that moves to the next line.")]
    [SerializeField] private Button continueButton;

    private IReadOnlyList<DialogueLine> lines;
    private int currentIndex;
    private Action onDone;

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Next);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnLanguageChanged += RefreshCurrentLine;
    }

    private void OnDisable()
    {
        GameEvents.OnLanguageChanged -= RefreshCurrentLine;
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // Starts showing a set of lines. Called by the DialogueManager.
    public void Show(IReadOnlyList<DialogueLine> dialogueLines, Action doneCallback)
    {
        lines = dialogueLines;
        onDone = doneCallback;
        currentIndex = 0;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (lines == null || lines.Count == 0)
        {
            Finish();
            return;
        }

        RefreshCurrentLine();
    }

    // Shows the next line, or finishes if there are no more.
    public void Next()
    {
        currentIndex++;

        if (lines == null || currentIndex >= lines.Count)
        {
            Finish();
            return;
        }

        RefreshCurrentLine();
    }

    // Redraws the current line in the player's language.
    private void RefreshCurrentLine()
    {
        if (lines == null || currentIndex < 0 || currentIndex >= lines.Count)
        {
            return;
        }

        DialogueLine line = lines[currentIndex];
        if (speakerText != null)
        {
            speakerText.text = Localize(line.speaker);
        }
        if (bodyText != null)
        {
            bodyText.text = Localize(line.text);
        }
    }

    // Hides the window and tells the DialogueManager the conversation is over.
    private void Finish()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        lines = null;
        Action callback = onDone;
        onDone = null;
        callback?.Invoke();
    }

    // Turns a LocalizedString into text in the current language.
    private string Localize(LocalizedString value)
    {
        if (GameManager.Instance == null || GameManager.Instance.Localization == null)
        {
            return value.english;
        }

        return GameManager.Instance.Localization.Get(value);
    }
}
