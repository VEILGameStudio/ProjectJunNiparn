// MessageInteractable
// An object that shows a short "mini dialogue" line when the player clicks it, like
// reading a sign or looking at scenery. It does not change anything in the game.
//
// Put this on: the object to inspect. It also needs a Collider (Collider2D in 2D
//   scenes, 3D Collider in 2.5D scenes) so the click ray can hit it.
// Assign in Inspector:
//   - Prompt: text shown to the player, e.g. "Look".
//   - Message: the short line to show.

using UnityEngine;

public class MessageInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player can inspect this, e.g. 'Look'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Message")]
    [Tooltip("The short line shown when the player interacts.")]
    [SerializeField] private LocalizedString message;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Shows the message in the mini dialogue popup.
    public void Interact(GameObject interactor)
    {
        if (MiniDialogueUI.Instance == null)
        {
            Debug.LogWarning($"MessageInteractable on '{name}': no MiniDialogueUI was found. Add one to your Canvas.", this);
            return;
        }

        MiniDialogueUI.Instance.ShowLocalized(message);
    }
}
