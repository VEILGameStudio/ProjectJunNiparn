// Inventory
// Holds everything the player is carrying. It can add items, remove items, and
// count them, and items stack up to each item's Max Stack. Whenever anything
// changes it fires OnInventoryChanged so the UI can refresh itself. The UI reads
// the inventory but never changes it directly - it calls Add/Remove here.
// The inventory survives scene changes and is saved with the game.
//
// Put this on: the "Managers" GameObject (so it lives for the whole game).
// Assign in Inspector: Item Database (used to rebuild items when loading a save).

using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : Singleton<Inventory>, ISaveParticipant
{
    [Header("Data")]
    [Tooltip("The database of all items, used to rebuild the inventory when loading a save.")]
    [SerializeField] private ItemDatabase itemDatabase;

    // The slots the player currently owns.
    private readonly List<InventorySlot> slots = new List<InventorySlot>();

    // The UI reads this to draw the slots. It cannot change the list.
    public IReadOnlyList<InventorySlot> Slots => slots;

    // Fired whenever items are added or removed. The UI listens to this.
    public event Action OnInventoryChanged;

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

    protected override void OnDestroy()
    {
        base.OnDestroy();
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
        OnInventoryChanged?.Invoke();
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

        OnInventoryChanged?.Invoke();
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

    // Shared add logic without firing the change event (used by Add and by loading).
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
    }

    // Rebuilds the inventory from the save data.
    public void RestoreState(GameSaveData data)
    {
        slots.Clear();

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
        }

        OnInventoryChanged?.Invoke();
    }
}
