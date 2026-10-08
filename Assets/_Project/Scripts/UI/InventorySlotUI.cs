// InventorySlotUI
// One clickable square in the inventory grid. It shows an item's icon and, if
// there is more than one, the count. Clicking it tells the InventoryUI which item
// was selected so it can show the name and description.
//
// Put this on: the Inventory Slot prefab (a UI Button).
// Assign in Inspector:
//   - Icon Image: the Image that shows the item picture.
//   - Count Text: the TextMeshPro text that shows the amount.
//   - Button: the Button on this slot (usually on this same object).

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("The Image that shows the item's icon.")]
    [SerializeField] private Image iconImage;

    [Tooltip("The TextMeshPro text that shows how many items are in the slot.")]
    [SerializeField] private TMP_Text countText;

    [Tooltip("The Button component on this slot.")]
    [SerializeField] private Button button;

    // The item this slot is currently showing.
    private ItemData item;

    // Called when this slot is clicked, with the item it holds.
    private Action<ItemData> onClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    // Fills this slot with an item and count, and remembers what to do on click.
    public void Show(InventorySlot slot, Action<ItemData> clickedCallback)
    {
        item = slot.Item;
        onClicked = clickedCallback;

        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = item.Icon != null;
        }

        if (countText != null)
        {
            // Only show a number when there is more than one.
            countText.text = slot.Count > 1 ? slot.Count.ToString() : string.Empty;
        }
    }

    // Tells the InventoryUI which item was clicked.
    private void HandleClick()
    {
        onClicked?.Invoke(item);
    }
}
