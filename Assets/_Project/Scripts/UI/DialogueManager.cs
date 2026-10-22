// DialogueManager
// The single entry point for showing dialogue. Other scripts call PlayFull,
// PlayChoice, or ShowMini here; this manager shows the right panel. For full and
// choice dialogues it raises GameEvents.OnDialogueStarted / OnDialogueEnded, and
// the GameManager locks player input in between.
// Other scripts reach it with: UIManager.Instance.Dialogue
//
// Put this on: a "DialogueManager" GameObject on your main (persistent) Canvas.
// Assign in Inspector:
//   - Full Dialogue UI: the FullDialogueUI panel.
//   - Choice Dialogue UI: the ChoiceDialogueUI panel.
//   (Mini dialogue uses the MiniDialogueUI from the UIManager automatically.)

using System;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The panel that shows full conversations (speaker + lines).")]
    [SerializeField] private FullDialogueUI fullDialogueUI;

    [Tooltip("The panel that shows a yes/no question.")]
    [SerializeField] private ChoiceDialogueUI choiceDialogueUI;

    // Remembered callbacks so we can run them when a dialogue ends.
    private Action fullEndCallback;
    private Action choiceYesCallback;
    private Action choiceNoCallback;

    // Plays a full conversation. onEnd runs after the last line is dismissed.
    public void PlayFull(FullDialogueData data, Action onEnd = null)
    {
        if (data == null || fullDialogueUI == null)
        {
            Debug.LogError("DialogueManager: cannot play a full dialogue. Check the data and that Full Dialogue UI is assigned.", this);
            onEnd?.Invoke();
            return;
        }

        fullEndCallback = onEnd;
        GameEvents.RaiseDialogueStarted();
        fullDialogueUI.Show(data.Lines, HandleFullEnded);
    }

    // Shows a yes/no question. onYes or onNo runs after the player chooses.
    public void PlayChoice(ChoiceDialogueData data, Action onYes, Action onNo)
    {
        if (data == null || choiceDialogueUI == null)
        {
            Debug.LogError("DialogueManager: cannot play a choice dialogue. Check the data and that Choice Dialogue UI is assigned.", this);
            return;
        }

        choiceYesCallback = onYes;
        choiceNoCallback = onNo;
        GameEvents.RaiseDialogueStarted();
        choiceDialogueUI.Show(data, HandleChoiceYes, HandleChoiceNo);
    }

    // Shows one short line in the mini popup. Does not lock the player.
    public void ShowMini(MiniDialogueData data)
    {
        if (data == null)
        {
            return;
        }
        if (UIManager.Instance == null || UIManager.Instance.MiniDialogue == null)
        {
            Debug.LogWarning("DialogueManager: no MiniDialogueUI found. Make sure the UIManager has one.", this);
            return;
        }

        UIManager.Instance.MiniDialogue.ShowLocalized(data.Text);
    }

    // Runs when the full dialogue finishes: unlock the player, then run the callback.
    private void HandleFullEnded()
    {
        GameEvents.RaiseDialogueEnded();
        Action callback = fullEndCallback;
        fullEndCallback = null;
        callback?.Invoke();
    }

    // Runs when the player picks Yes.
    private void HandleChoiceYes()
    {
        GameEvents.RaiseDialogueEnded();
        Action callback = choiceYesCallback;
        ClearChoiceCallbacks();
        callback?.Invoke();
    }

    // Runs when the player picks No.
    private void HandleChoiceNo()
    {
        GameEvents.RaiseDialogueEnded();
        Action callback = choiceNoCallback;
        ClearChoiceCallbacks();
        callback?.Invoke();
    }

    private void ClearChoiceCallbacks()
    {
        choiceYesCallback = null;
        choiceNoCallback = null;
    }
}
