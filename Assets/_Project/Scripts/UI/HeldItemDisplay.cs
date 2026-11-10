// HeldItemDisplay
// Shows the icon of the item the player is holding. Put it on any UI Image and that
// Image always shows the held item: the small hand slot next to the bag button uses
// it, and a puzzle screen can use it to show "the thing you brought".
// While the hand is empty the Image shows the Empty Sprite, or hides if there is none.
//
// Put this on: a UI Image (the one that should show the held item's icon).
// Assign in Inspector: Empty Sprite (optional).

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HeldItemDisplay : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Optional picture shown while the hand is empty. Leave empty to hide the Image instead.")]
    [SerializeField] private Sprite emptySprite;

    private Image iconImage;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        GameEvents.OnHeldItemChanged += Show;

        // This may be switched on long after the last change was announced (a puzzle
        // screen that opens later), so look at what is held right now as well.
        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
        {
            Show(GameManager.Instance.Inventory.HeldItem);
        }
        else
        {
            Show(null);
        }
    }

    private void OnDisable()
    {
        GameEvents.OnHeldItemChanged -= Show;
    }

    // Shows the icon of this item, or the empty look when nothing is held.
    private void Show(ItemData heldItem)
    {
        Sprite sprite = emptySprite;
        if (heldItem != null && heldItem.Icon != null)
        {
            sprite = heldItem.Icon;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }
}
