// DialogueInteractable
// Put this on something the player can use to start a full conversation (an NPC,
// a note, a grandpa). When the conversation ends, it can run extra actions through
// the On Dialogue End event (for example start a cutscene or give a quest).
//
// Put this on: the object to talk to. Add a Collider2D first (for example a BoxCollider2D).
// Assign in Inspector:
//   - Dialogue: the FullDialogueData to play.
//   - On Dialogue End (optional): actions to run after the conversation.
//   - Activation / Max Click Distance / One Shot / Required Item: see InteractableBase.

using UnityEngine;
using UnityEngine.Events;

public class DialogueInteractable : InteractableBase
{
    [Header("Dialogue")]
    [Tooltip("The full conversation to play.")]
    [SerializeField] private FullDialogueData dialogue;

    [Header("Events")]
    [Tooltip("Runs after the conversation finishes. Leave empty if not needed.")]
    [SerializeField] private UnityEvent onDialogueEnd;

    // Starts the conversation.
    protected override void OnInteract(PlayerContext player)
    {
        if (dialogue == null)
        {
            Debug.LogError($"DialogueInteractable on '{name}': Dialogue is not assigned. Drag a FullDialogueData asset here.", this);
            return;
        }
        if (UIManager.Instance == null || UIManager.Instance.Dialogue == null)
        {
            Debug.LogError($"DialogueInteractable on '{name}': no DialogueManager found. Make sure the PersistentCanvas (with a UIManager) is in the scene.", this);
            return;
        }

        UIManager.Instance.Dialogue.PlayFull(dialogue, RaiseDialogueEnd);
    }

    // Runs the On Dialogue End actions after the conversation.
    private void RaiseDialogueEnd()
    {
        onDialogueEnd?.Invoke();
    }
}
