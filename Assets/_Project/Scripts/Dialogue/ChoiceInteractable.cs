// ChoiceInteractable
// Put this on something that asks the player a yes/no question, like
// "Take the gem? Yes / No". You wire what happens for each answer here in the
// Inspector: for example On Yes -> add item + destroy object, On No -> do nothing.
//
// Put this on: the object that asks the question. Add a Collider2D first (for
//   example a BoxCollider2D).
// Assign in Inspector:
//   - Choice: the ChoiceDialogueData (the question text).
//   - On Yes / On No: the actions for each answer.
//   - Activation / Max Click Distance / One Shot / Required Item: see InteractableBase.

using UnityEngine;
using UnityEngine.Events;

public class ChoiceInteractable : InteractableBase
{
    [Header("Choice")]
    [Tooltip("The yes/no question to ask.")]
    [SerializeField] private ChoiceDialogueData choice;

    [Header("Events")]
    [Tooltip("Runs when the player picks Yes.")]
    [SerializeField] private UnityEvent onYes;

    [Tooltip("Runs when the player picks No.")]
    [SerializeField] private UnityEvent onNo;

    // Asks the question.
    protected override void OnInteract(PlayerContext player)
    {
        if (choice == null)
        {
            Debug.LogError($"ChoiceInteractable on '{name}': Choice is not assigned. Drag a ChoiceDialogueData asset here.", this);
            return;
        }
        if (UIManager.Instance == null || UIManager.Instance.Dialogue == null)
        {
            Debug.LogError($"ChoiceInteractable on '{name}': no DialogueManager found. Make sure the PersistentCanvas (with a UIManager) is in the scene.", this);
            return;
        }

        UIManager.Instance.Dialogue.PlayChoice(choice, InvokeYes, InvokeNo);
    }

    // Runs the On Yes actions.
    private void InvokeYes()
    {
        onYes?.Invoke();
    }

    // Runs the On No actions.
    private void InvokeNo()
    {
        onNo?.Invoke();
    }
}
