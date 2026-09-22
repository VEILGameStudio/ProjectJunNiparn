// DialogueInteractable
// Put this on something the player can click to start a full conversation (an NPC,
// a note, a grandpa). When the conversation ends, it can run extra actions through
// the On Dialogue End event (for example start a cutscene or give a quest).
//
// Put this on: the object to talk to. It also needs a Collider (Collider2D in 2D
//   scenes, 3D Collider in 2.5D scenes) so the click ray can hit it.
// Assign in Inspector:
//   - Prompt: text shown to the player, e.g. "Talk".
//   - Dialogue: the FullDialogueData to play.
//   - On Dialogue End (optional): actions to run after the conversation.

using UnityEngine;
using UnityEngine.Events;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player can talk, e.g. 'Talk'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Dialogue")]
    [Tooltip("The full conversation to play when clicked.")]
    [SerializeField] private FullDialogueData dialogue;

    [Header("Events")]
    [Tooltip("Runs after the conversation finishes. Leave empty if not needed.")]
    [SerializeField] private UnityEvent onDialogueEnd;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Starts the conversation when the player interacts.
    public void Interact(GameObject interactor)
    {
        if (dialogue == null)
        {
            Debug.LogError($"DialogueInteractable on '{name}': Dialogue is not assigned. Drag a FullDialogueData asset here.", this);
            return;
        }
        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"DialogueInteractable on '{name}': no DialogueManager found. Add one to your Canvas.", this);
            return;
        }

        DialogueManager.Instance.PlayFull(dialogue, RaiseDialogueEnd);
    }

    // Runs the On Dialogue End actions after the conversation.
    private void RaiseDialogueEnd()
    {
        onDialogueEnd?.Invoke();
    }
}
