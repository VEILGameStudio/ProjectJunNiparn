// ItemData
// The description of one kind of item (a key, a coin, a puzzle gem, and so on).
// It holds no live data - it is a template. The Inventory stores how many of each
// ItemData the player owns. Create one asset per item type.
//
// Create an item asset with:
//   Right-click in Project > Create > Game > Item
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: Id, Display Name, Description, Icon, Max Stack.

using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("A unique short id used in save files, for example 'rusty_key'. Never change it after items are saved.")]
    [SerializeField] private string id;

    [Header("Text (shown to the player)")]
    [Tooltip("The item name in each language.")]
    [SerializeField] private LocalizedString displayName;

    [Tooltip("The item description in each language.")]
    [SerializeField] private LocalizedString description;

    [Header("Appearance")]
    [Tooltip("The picture shown in the inventory.")]
    [SerializeField] private Sprite icon;

    [Header("Stacking")]
    [Tooltip("How many of this item fit in one inventory slot. Use 1 for items that never stack.")]
    [SerializeField] private int maxStack = 1;

    // The unique save id for this item.
    public string Id => id;

    // The item name in every language (turn into text with LocalizationManager).
    public LocalizedString DisplayName => displayName;

    // The item description in every language.
    public LocalizedString Description => description;

    // The inventory picture.
    public Sprite Icon => icon;

    // The most that fit in one slot.
    public int MaxStack => maxStack;

    // Keeps the values sensible while editing in the Inspector.
    private void OnValidate()
    {
        if (maxStack < 1)
        {
            maxStack = 1;
        }
    }
}
