// IInteractable
// The shared contract for anything the player can click on or walk into: pickups,
// doors, signs, NPCs, and so on. The PlayerInteractor talks to every interactable
// through this, without needing to know what kind of thing it is.
//
// Do not implement this directly. Subclass InteractableBase instead - it already
// does all the shared checks.
//
// Put this on: nothing directly. InteractableBase implements it.

public interface IInteractable
{
    // How this is started: Click, Touch, or Both.
    ActivationMode Activation { get; }

    // True when the player may use this right now (close enough, still switched on).
    bool CanInteract(PlayerContext player);

    // Uses this object. The PlayerInteractor only calls it after CanInteract is true.
    void Interact(PlayerContext player);
}
