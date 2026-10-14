// Inventory
// Holds everything the player is carrying. It can add items, remove items, and
// count them, and items stack up to each item's Max Stack. Whenever anything
// changes it raises GameEvents.OnInventoryChanged so the UI can refresh itself.
// The UI never changes the inventory directly - only Add/Remove here do.
// The inventory survives scene changes and is saved with the game.
// Other scripts reach it with: GameManager.Instance.Inventory
// (inside an interactable, use player.Inventory instead).
//
// Holding: the player can hold ONE item in the hand. Holding does not take the item
// out of the bag (Count stays the same), and the hand empties itself when the last
// of that item is gone. A puzzle asks "is the player holding the right thing?" with:
//   if (player.Inventory.IsHolding(keyItem)) { ... }
// Every change raises GameEvents.OnHeldItemChanged.
//
// Put this on: the "Managers" GameObject (next to the GameManager).
// Assign in Inspector: Item Database (used to rebuild items when loading a save).

using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour, ISaveParticipant
{
    [Header("Data")]
    [Tooltip("The database of all items, used to rebuild the inventory when loading a save.")]
    [SerializeField] private ItemDatabase itemDatabase;

    // The slots the player currently owns.
    private readonly List<InventorySlot> slots = new List<InventorySlot>();

    // The item in the player's hand. Null while the hand is empty.
    private ItemData heldItem;

    // The current slots, read only.
    public IReadOnlyList<InventorySlot> Slots => slots;

    // The item in the player's hand, or null while the hand is empty.
    public ItemData HeldItem => heldItem;

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
        if (itemDatabase == null)
        {
            Debug.LogError("Inventory on 'Managers': Item Database is not assigned. Drag your ItemDatabase asset here so saves can load.", this);
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    // Adds items to the inventory, filling existing stacks first. Always succeeds
    // (it makes new stacks as needed). Returns false only for bad input.
    public bool Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        AddInternal(item, amount);
        GameEvents.RaiseInventoryChanged(slots);
        return true;
    }

    // Removes items from the inventory. Returns false if the player did not have
    // enough (in that case nothing is removed).
    public bool Remove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || Count(item) < amount)
        {
            return false;
        }

        int remaining = amount;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].Item != item)
            {
                continue;
            }

            int taken = Mathf.Min(slots[i].Count, remaining);
            slots[i].ChangeCount(-taken);
            remaining -= taken;

            if (slots[i].Count <= 0)
            {
                slots.RemoveAt(i);
            }
        }

        // The last one is gone, so it cannot stay in the hand.
        if (heldItem != null && !Has(heldItem))
        {
            SetHeldItem(null);
        }

        GameEvents.RaiseInventoryChanged(slots);
        return true;
    }

    // Counts how many of an item the player has across all stacks.
    public int Count(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
        {
            if (slot.Item == item)
            {
                total += slot.Count;
            }
        }
        return total;
    }

    // True if the player has at least this many of the item.
    public bool Has(ItemData item, int amount = 1)
    {
        return Count(item) >= amount;
    }

    // Takes an item from the bag into the hand. Only one item is held at a time, so this
    // replaces whatever was held. Returns false if the player does not have the item.
    public bool Hold(ItemData item)
    {
        if (item == null || !Has(item))
        {
            return false;
        }

        SetHeldItem(item);
        return true;
    }

    // Empties the hand. The item stays in the bag.
    public void StopHolding()
    {
        SetHeldItem(null);
    }

    // True if this item is in the player's hand right now.
    public bool IsHolding(ItemData item)
    {
        return item != null && heldItem == item;
    }

    // Changes the held item and announces it, but only when it really changed.
    private void SetHeldItem(ItemData item)
    {
        if (heldItem == item)
        {
            return;
        }

        heldItem = item;
        GameEvents.RaiseHeldItemChanged(heldItem);
    }

    // Shared add logic without raising the change event (used by Add and by loading).
    private void AddInternal(ItemData item, int amount)
    {
        int remaining = amount;

        // Fill stacks that already exist and still have room.
        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }
            if (slot.Item != item || slot.Count >= item.MaxStack)
            {
                continue;
            }

            int space = item.MaxStack - slot.Count;
            int added = Mathf.Min(space, remaining);
            slot.ChangeCount(added);
            remaining -= added;
        }

        // Make new stacks for whatever is left over.
        while (remaining > 0)
        {
            int added = Mathf.Min(item.MaxStack, remaining);
            slots.Add(new InventorySlot(item, added));
            remaining -= added;
        }
    }

    // --- Saving (ISaveParticipant) ---

    // Copies the current items into the save data.
    public void CaptureState(GameSaveData data)
    {
        data.inventoryEntries.Clear();
        foreach (InventorySlot slot in slots)
        {
            data.inventoryEntries.Add(new InventoryEntry { itemId = slot.Item.Id, count = slot.Count });
        }

        data.heldItemId = heldItem != null ? heldItem.Id : string.Empty;
    }

    // Rebuilds the inventory, and what was in the hand, from the save data.
    public void RestoreState(GameSaveData data)
    {
        slots.Clear();
        ItemData savedHeldItem = null;

        if (itemDatabase != null)
        {
            foreach (InventoryEntry entry in data.inventoryEntries)
            {
                ItemData item = itemDatabase.GetById(entry.itemId);
                if (item != null)
                {
                    AddInternal(item, entry.count);
                }
            }

            // A save made before holding existed has no id here: the hand is simply empty.
            if (!string.IsNullOrEmpty(data.heldItemId))
            {
                savedHeldItem = itemDatabase.GetById(data.heldItemId);
            }
        }

        // Only something that is really in the bag can be held.
        if (savedHeldItem != null && !Has(savedHeldItem))
        {
            savedHeldItem = null;
        }

        GameEvents.RaiseInventoryChanged(slots);
        SetHeldItem(savedHeldItem);
    }
}
