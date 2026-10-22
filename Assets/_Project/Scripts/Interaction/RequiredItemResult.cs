// RequiredItemResult
// The three answers to "may the player use this?" when an interactable needs an item:
//   Ready   - nothing is needed, or the player has (and, if asked, is holding) the item.
//   Missing - the player does not have the item at all.
//   NotHeld - the player has the item in the bag, but must be holding it.
//
// Put this on: nothing. It is used by InteractableBase.

public enum RequiredItemResult
{
    Ready,
    Missing,
    NotHeld
}
