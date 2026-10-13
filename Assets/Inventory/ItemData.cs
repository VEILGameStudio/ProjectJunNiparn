// ItemData
// The description of one kind of item (a key, a coin, a puzzle gem, and so on).
// It holds no live data - it is a template. The Inventory stores how many of each
// ItemData the player owns. Create one asset per item type.
//
// Create an item asset with:
//   Right-click in Project > Create > Game > Item
// Then do both of these, or the game cannot find the item:
//   1. Keep the asset in the folder of its Category: Data/Items/Puzzle, Consumable,
//      Quest or Secret.
//   2. Drag the asset into the Items list of the ItemDatabase asset.
// Never change an item's Id after the game has been saved with it: save files find
// items by Id. The names and descriptions can be changed at any time.
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: Id, Category, Display Name, Description, Icon, Max Stack.
//   Category = Consumable: also Heal Amount and Stamina Amount.
//   Category = Secret: also Secret Title, Secret Body and (optional) Secret Image.

using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("A unique short id used in save files, for example 'rusty_key'. Never change it after items are saved.")]
    [SerializeField] private string id;

    [Tooltip("The kind of item. It must match the folder this asset is in: Data/Items/Puzzle, Consumable, Quest or Secret.")]
    [SerializeField] private ItemCategory category;

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

    [Header("Consumable only")]
    [Tooltip("Health given when the item is used. 0 = no effect. Only for Category = Consumable.")]
    [SerializeField] private int healAmount;

    [Tooltip("Stamina given when the item is used. 0 = no effect. Only for Category = Consumable.")]
    [SerializeField] private float staminaAmount;

    [Header("Secret only (shown in the gallery)")]
    [Tooltip("The title of this secret in the gallery, in each language. Only for Category = Secret.")]
    [SerializeField] private LocalizedString secretTitle;

    [Tooltip("The text of this secret in the gallery, in each language. Only for Category = Secret.")]
    [SerializeField] private LocalizedString secretBody;

    [Tooltip("Optional. A picture shown with this secret in the gallery. Only for Category = Secret.")]
    [SerializeField] private Sprite secretImage;

    // The unique save id for this item.
    public string Id => id;

    // The kind of item (Puzzle, Consumable, Quest or Secret).
    public ItemCategory Category => category;

    // The item name in every language (turn into text with LocalizationManager).
    public LocalizedString DisplayName => displayName;

    // The item description in every language.
    public LocalizedString Description => description;

    // The inventory picture.
    public Sprite Icon => icon;

    // The most that fit in one slot.
    public int MaxStack => maxStack;

    // Consumables: health given when used. 0 = no effect.
    public int HealAmount => healAmount;

    // Consumables: stamina given when used. 0 = no effect.
    public float StaminaAmount => staminaAmount;

    // Secrets: the gallery title in every language.
    public LocalizedString SecretTitle => secretTitle;

    // Secrets: the gallery text in every language.
    public LocalizedString SecretBody => secretBody;

    // Secrets: the gallery picture. May be empty.
    public Sprite SecretImage => secretImage;

    // Keeps the values sensible while editing in the Inspector.
    private void OnValidate()
    {
        if (maxStack < 1)
        {
            maxStack = 1;
        }
        if (healAmount < 0)
        {
            healAmount = 0;
        }
        if (staminaAmount < 0f)
        {
            staminaAmount = 0f;
        }
    }
}
