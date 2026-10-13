// PickupItem
// An object in the world that the player can pick up. It adds the item to the
// inventory, raises GameEvents.OnItemPickedUp (the SaveManager autosaves on that),
// optionally shows a short message, then removes itself from the scene.
// A pickup is always used only once: the save file remembers it by its Unique Id, so
// it is not there any more after loading a save.
// Use Interact Sound (from InteractableBase) for the pickup sound.
//
// Put this on: the pickup GameObject. Add a BoxCollider click box first
//   (Is Trigger ticked, layer Interactable) - see InteractableBase for the full setup.
// Assign in Inspector:
//   - Item: the ItemData to give the player.
//   - Amount: how many to give.
//   - Unique Id (from InteractableBase): a short name no other object in this scene has,
//     for example 'corridor_key'. Without it the pickup comes back after loading a save.
//   - Show Message On Pickup / Pickup Message (optional): a short line shown after picking up.
//   - Activation / Interact Sound / Required Item: see InteractableBase.

using UnityEngine;

public class PickupItem : InteractableBase
{
    [Header("Item")]
    [Tooltip("The item that is added to the inventory.")]
    [SerializeField] private ItemData item;

    [Tooltip("How many of the item to add.")]
    [SerializeField] private int amount = 1;

    [Header("Message (optional)")]
    [Tooltip("If ticked, a short message is shown after picking up.")]
    [SerializeField] private bool showMessageOnPickup = true;

    [Tooltip("The short message shown after picking up, e.g. 'Got a rusty key'.")]
    [SerializeField] private LocalizedString pickupMessage;

    // A pickup is always used only once, so the save file must remember it.
    protected override bool UsedOnlyOnce => true;

    // Adds the item to the inventory, gives feedback, then removes this object.
    protected override void OnInteract(PlayerContext player)
    {
        if (item == null)
        {
            Debug.LogError($"PickupItem on '{name}': Item is not assigned. Drag an ItemData asset into the Item field.", this);
            return;
        }
        if (player.Inventory == null)
        {
            Debug.LogError($"PickupItem on '{name}': no Inventory was found. Make sure the Managers object (with an Inventory) is in the scene.", this);
            return;
        }

        player.Inventory.Add(item, amount);
        MarkUsed(); // Before the pickup event, so the autosave it starts already knows this is gone.
        GameEvents.RaiseItemPickedUp(item, amount);

        if (showMessageOnPickup)
        {
            ShowMiniMessage(pickupMessage);
        }

        Destroy(gameObject);
    }

    // In a loaded save where this was already picked up, it is not in the scene at all.
    protected override void OnRestoredAsUsed()
    {
        gameObject.SetActive(false);
    }
}
