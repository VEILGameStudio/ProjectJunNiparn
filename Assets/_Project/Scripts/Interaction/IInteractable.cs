// IInteractable
// The shared contract for anything the player can click on: pickups, doors,
// signs, NPCs, and so on. The player's interaction script calls Interact() on
// whatever it clicked, without needing to know what kind of thing it is.
//
// Put this on: nothing directly. Scripts implement this interface, e.g.:
//   public class Door : MonoBehaviour, IInteractable { ... }

using UnityEngine;

public interface IInteractable
{
    // A short line shown to the player, like "Open" or "Pick up". It is a
    // LocalizedString so it can be translated.
    LocalizedString Prompt { get; }

    // Runs when the player interacts with this object. "interactor" is usually
    // the player GameObject.
    void Interact(GameObject interactor);
}
