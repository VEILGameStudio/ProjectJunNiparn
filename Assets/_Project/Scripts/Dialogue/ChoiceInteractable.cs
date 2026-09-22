// ChoiceInteractable
// Put this on something the player can click to be asked a yes/no question, like
// "Take the gem? Yes / No". You wire what happens for each answer here in the
// Inspector: for example On Yes -> add item + destroy object, On No -> do nothing.
//
// Put this on: the object that asks the question. It also needs a Collider
//   (Collider2D in 2D scenes, 3D Collider in 2.5D scenes) so the click ray can hit it.
// Assign in Inspector:
//   - Prompt: text shown to the player, e.g. "Take".
//   - Choice: the ChoiceDialogueData (the question text).
//   - On Yes / On No: the actions for each answer.

using UnityEngine;
using UnityEngine.Events;

public class ChoiceInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player points at this, e.g. 'Take'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Choice")]
    [Tooltip("The yes/no question to ask.")]
    [SerializeField] private ChoiceDialogueData choice;

    [Header("Events")]
    [Tooltip("Runs when the player picks Yes.")]
    [SerializeField] private UnityEvent onYes;

    [Tooltip("Runs when the player picks No.")]
    [SerializeField] private UnityEvent onNo;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Asks the question when the player interacts.
    public void Interact(GameObject interactor)
    {
        if (choice == null)
        {
            Debug.LogError($"ChoiceInteractable on '{name}': Choice is not assigned. Drag a ChoiceDialogueData asset here.", this);
            return;
        }
        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"ChoiceInteractable on '{name}': no DialogueManager found. Add one to your Canvas.", this);
            return;
        }

        DialogueManager.Instance.PlayChoice(choice, InvokeYes, InvokeNo);
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
