// ChoiceDialogueUI
// Shows a yes/no question with two buttons. When the player picks one, it hides
// itself and tells the DialogueManager which button was chosen. The DialogueManager
// drives this panel - do not call it directly from gameplay.
//
// Put this on: a "ChoiceDialogueUI" panel under your main Canvas.
// Assign in Inspector:
//   - Panel: the window GameObject to show/hide.
//   - Question Text: TextMeshPro text for the question.
//   - Yes Button / No Button: the two choice buttons.
//   - Yes Label / No Label: the TextMeshPro texts on those buttons.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceDialogueUI : MonoBehaviour
{
    [Header("Window")]
    [Tooltip("The question window that is shown or hidden.")]
    [SerializeField] private GameObject panel;

    [Header("Text")]
    [Tooltip("TextMeshPro text that shows the question.")]
    [SerializeField] private TMP_Text questionText;

    [Header("Buttons")]
    [Tooltip("The Yes button.")]
    [SerializeField] private Button yesButton;

    [Tooltip("The No button.")]
    [SerializeField] private Button noButton;

    [Tooltip("TextMeshPro label on the Yes button.")]
    [SerializeField] private TMP_Text yesLabel;

    [Tooltip("TextMeshPro label on the No button.")]
    [SerializeField] private TMP_Text noLabel;

    private ChoiceDialogueData currentData;
    private Action onYes;
    private Action onNo;

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(ChooseYes);
        }
        if (noButton != null)
        {
            noButton.onClick.AddListener(ChooseNo);
        }
    }

    private void OnEnable()
    {
        GameEvents.LanguageChanged += RefreshText;
    }

    private void OnDisable()
    {
        GameEvents.LanguageChanged -= RefreshText;
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // Shows the question and remembers what to do for each answer.
    public void Show(ChoiceDialogueData data, Action yesCallback, Action noCallback)
    {
        currentData = data;
        onYes = yesCallback;
        onNo = noCallback;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        RefreshText();
    }

    // Redraws the question and button labels in the player's language.
    private void RefreshText()
    {
        if (currentData == null)
        {
            return;
        }

        if (questionText != null)
        {
            questionText.text = Localize(currentData.Question);
        }
        if (yesLabel != null)
        {
            yesLabel.text = Localize(currentData.YesLabel);
        }
        if (noLabel != null)
        {
            noLabel.text = Localize(currentData.NoLabel);
        }
    }

    // Called by the Yes button.
    private void ChooseYes()
    {
        Close();
        Action callback = onYes;
        ClearCallbacks();
        callback?.Invoke();
    }

    // Called by the No button.
    private void ChooseNo()
    {
        Close();
        Action callback = onNo;
        ClearCallbacks();
        callback?.Invoke();
    }

    private void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        currentData = null;
    }

    private void ClearCallbacks()
    {
        onYes = null;
        onNo = null;
    }

    // Turns a LocalizedString into text in the current language.
    private string Localize(LocalizedString value)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(value)
            : value.english;
    }
}
