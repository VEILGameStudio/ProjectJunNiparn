// InventorySlot
// One line in the inventory: which item, and how many of it. The Inventory owns a
// list of these. The UI reads them but never changes them - only the Inventory
// class changes the counts.
//
// Put this on: nothing. It is a small data class used by the Inventory.

public class InventorySlot
{
    // The item held in this slot.
    public ItemData Item { get; private set; }

    // How many of the item are in this slot.
    public int Count { get; private set; }

    public InventorySlot(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    // Adds to or removes from the count. Only the Inventory should call this.
    public void ChangeCount(int amount)
    {
        Count += amount;
    }
}
