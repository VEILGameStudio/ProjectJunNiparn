// ItemDatabase
// A list of every item in the game. When the game loads a save file, it only has
// each item's id (text), so it uses this database to find the matching ItemData
// asset again. Keep every ItemData asset in this list.
//
// Create the database asset with:
//   Right-click in Project > Create > Game > Item Database
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: drag every ItemData asset into the Items list.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    [Tooltip("Drag every ItemData asset in the game into this list.")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    // Fast id -> item lookup, built the first time it is needed.
    private Dictionary<string, ItemData> lookup;

    // Finds the item with this id, or returns null if it is not in the list.
    public ItemData GetById(string id)
    {
        BuildLookupIfNeeded();

        if (lookup.TryGetValue(id, out ItemData item))
        {
            return item;
        }

        Debug.LogWarning($"ItemDatabase: no item found with id '{id}'. Add its ItemData asset to the Items list.", this);
        return null;
    }

    // Builds the lookup dictionary from the Items list.
    private void BuildLookupIfNeeded()
    {
        if (lookup != null)
        {
            return;
        }

        lookup = new Dictionary<string, ItemData>();
        foreach (ItemData item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
            {
                continue;
            }
            lookup[item.Id] = item;
        }
    }
}
