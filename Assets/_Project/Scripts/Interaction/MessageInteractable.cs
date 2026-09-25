// MessageInteractable
// An object that shows a short "mini dialogue" line when the player uses it, like
// reading a sign or looking at scenery. It does not change anything in the game.
//
// Put this on: the object to inspect. Add a Collider2D first (for example a BoxCollider2D).
// Assign in Inspector:
//   - Message: the short line to show.
//   - Activation / Max Click Distance / One Shot: see InteractableBase.

using UnityEngine;

public class MessageInteractable : InteractableBase
{
    [Header("Message")]
    [Tooltip("The short line shown when the player interacts.")]
    [SerializeField] private LocalizedString message;

    // Shows the message in the mini dialogue popup.
    protected override void OnInteract(PlayerContext player)
    {
        ShowMiniMessage(message);
    }
}
