// PlayerContext
// A small bundle of the player things an interactable may need: where the player
// is, the inventory, and the player's health. The PlayerInteractor builds one and
// hands it to every interactable, so interactables never search for the player.
//
// Put this on: nothing. It is a small data class.

using UnityEngine;

public class PlayerContext
{
    // The player's Transform (use it for the player's position).
    public Transform Transform { get; }

    // The inventory the player carries. Null if there is no Inventory in the game.
    public Inventory Inventory { get; }

    // The player's health. Null if the player has no PlayerHealth.
    public PlayerHealth Health { get; }

    public PlayerContext(Transform transform, Inventory inventory, PlayerHealth health)
    {
        Transform = transform;
        Inventory = inventory;
        Health = health;
    }
}
