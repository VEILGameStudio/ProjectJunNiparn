// ItemDatabase
// The list of every item in the whole game. There is exactly one of these: items travel
// between levels (a key found in level 3 is still carried in level 7), so the game must
// be able to find any item from any scene. Never make a second database, one per level
// or one per category.
// When the game loads a save file it only has each item's id (text), and it uses this
// database to find the matching ItemData asset again. An item that is not in this list
// cannot be loaded from a save.
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

    // Every item in the game, read only.
    public IReadOnlyList<ItemData> Items => items;

    // Finds the item with this id. If there is no such item (for example one that was removed
    // from the game after a save was made) it says so in the Console and returns null, so the
    // caller can skip it. It never stops the game.
    public ItemData GetById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("ItemDatabase: an item was asked for with an empty id, so it was skipped. A save file or a script did not give an id.", this);
            return null;
        }

        BuildLookupIfNeeded();

        if (lookup.TryGetValue(id, out ItemData item))
        {
            return item;
        }

        Debug.LogWarning($"ItemDatabase: no item found with id '{id}', so it was skipped. Add its ItemData asset to the Items list. (If the item was removed from the game on purpose, this is only a note.)", this);
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
            if (lookup.TryGetValue(item.Id, out ItemData firstItem))
            {
                Debug.LogError($"ItemDatabase: '{firstItem.name}' and '{item.name}' both have the id '{item.Id}'. A save file could load the wrong one. Give each item its own Id.", this);
                continue;
            }
            lookup.Add(item.Id, item);
        }
    }

    // The list was changed in the Inspector: build the lookup again the next time it is needed.
    private void OnValidate()
    {
        lookup = null;
    }
}
