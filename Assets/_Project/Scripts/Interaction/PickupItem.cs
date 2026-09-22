// PickupItem
// An object in the world that the player can click to pick up. It adds the item to
// the inventory, optionally shows a short message and plays a sound, then removes
// itself from the scene.
//
// Put this on: the pickup GameObject. It also needs a Collider (a Collider2D in 2D
//   scenes, or a 3D Collider in 2.5D scenes) so the click ray can hit it.
// Assign in Inspector:
//   - Prompt: text shown to the player, e.g. "Pick up".
//   - Item: the ItemData to give the player.
//   - Amount: how many to give.
//   - Pickup Message (optional): a short line shown after picking up.
//   - Pickup Sound (optional): a sound played on pickup.

using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player can pick this up, e.g. 'Pick up'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Item")]
    [Tooltip("The item that is added to the inventory.")]
    [SerializeField] private ItemData item;

    [Tooltip("How many of the item to add.")]
    [SerializeField] private int amount = 1;

    [Header("Feedback (optional)")]
    [Tooltip("If ticked, a short message is shown after picking up.")]
    [SerializeField] private bool showMessageOnPickup = true;

    [Tooltip("The short message shown after picking up, e.g. 'Got a rusty key'.")]
    [SerializeField] private LocalizedString pickupMessage;

    [Tooltip("Sound played when the item is picked up.")]
    [SerializeField] private AudioClip pickupSound;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Adds the item to the inventory, gives feedback, then removes this object.
    public void Interact(GameObject interactor)
    {
        if (item == null)
        {
            Debug.LogError($"PickupItem on '{name}': Item is not assigned. Drag an ItemData asset into the Item field.", this);
            return;
        }
        if (Inventory.Instance == null)
        {
            Debug.LogError($"PickupItem on '{name}': no Inventory was found. Make sure the Managers prefab is in the scene.", this);
            return;
        }

        Inventory.Instance.Add(item, amount);

        // Autosave right after picking something up.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(pickupSound);
        }
        if (showMessageOnPickup && MiniDialogueUI.Instance != null)
        {
            MiniDialogueUI.Instance.ShowLocalized(pickupMessage);
        }

        Destroy(gameObject);
    }
}
