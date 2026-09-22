// BlockedInteractable
// An object the player cannot use yet. When clicked, it explains why - for example
// "It is locked. I need a key." Use this for items or doors that become available
// after the player solves something. When the puzzle is solved, you can disable
// this component and enable the real interactable (like a PickupItem).
//
// Put this on: the blocked object. It also needs a Collider (Collider2D in 2D
//   scenes, 3D Collider in 2.5D scenes) so the click ray can hit it.
// Assign in Inspector:
//   - Prompt: text shown to the player, e.g. "Examine".
//   - Blocked Message: the reason it cannot be used yet.

using UnityEngine;

public class BlockedInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player points at this, e.g. 'Examine'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Message")]
    [Tooltip("The reason this cannot be used yet, e.g. 'It is locked. I need a key.'")]
    [SerializeField] private LocalizedString blockedMessage;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Shows the "cannot use yet" message in the mini dialogue popup.
    public void Interact(GameObject interactor)
    {
        if (MiniDialogueUI.Instance == null)
        {
            Debug.LogWarning($"BlockedInteractable on '{name}': no MiniDialogueUI was found. Add one to your Canvas.", this);
            return;
        }

        MiniDialogueUI.Instance.ShowLocalized(blockedMessage);
    }
}
