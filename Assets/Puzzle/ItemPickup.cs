using UnityEngine;

public class ItemPickup : InteractableBase
{
    [Header("Pickup")]
    [Tooltip("The item the player gets.")]
    [SerializeField] private ItemData item;

    [SerializeField] private int amount = 1;

    // A pickup is always used only once, and the save file remembers it.
    protected override bool UsedOnlyOnce => true;

    protected override void OnInteract(PlayerContext player)
    {
        if (item == null || player == null || player.Inventory == null)
        {
            return;
        }

        if (!player.Inventory.Add(item, amount))
        {
            return;
        }

        MarkUsed();
        gameObject.SetActive(false);   // เก็บแล้ว รูปหายจากฉาก
    }

    // After loading a save where this was already picked up, keep it hidden.
    protected override void OnRestoredAsUsed()
    {
        base.OnRestoredAsUsed();
        gameObject.SetActive(false);
    }
}