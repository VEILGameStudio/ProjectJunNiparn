// InventoryUI
// Shows the player's bag. Press the Inventory button to open or close it (this
// also freezes the game while open). It draws one slot per item using the slot
// prefab, and clicking a slot shows that item's name and description. It listens
// to GameEvents.OnInventoryChanged and redraws itself - it never changes the
// inventory data.
//
// Put this on: an "InventoryUI" GameObject under your main Canvas that is ALWAYS
//   active. Do NOT put it on the Panel it shows and hides, or the Inventory button
//   stops working once the panel is hidden.
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Panel: the inventory window GameObject to show/hide.
//   - Slot Container: the parent (with a Grid Layout Group) that slots go into.
//   - Slot Prefab: the Inventory Slot prefab (has an InventorySlotUI).
//   - Name Text / Description Text: TextMeshPro texts for the selected item.

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Windows")]
    [Tooltip("The inventory window that is shown or hidden.")]
    [SerializeField] private GameObject panel;

    [Header("Slots")]
    [Tooltip("The parent object (with a Grid Layout Group) that the slots are placed in.")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("The Inventory Slot prefab used for each item.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Selected Item Details")]
    [Tooltip("TextMeshPro text that shows the selected item's name.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("TextMeshPro text that shows the selected item's description.")]
    [SerializeField] private TMP_Text descriptionText;

    // The slot views currently on screen, so we can clear them on refresh.
    private readonly List<InventorySlotUI> spawnedSlots = new List<InventorySlotUI>();

    // The latest slots sent by GameEvents.OnInventoryChanged.
    private IReadOnlyList<InventorySlot> currentSlots;

    // The item the player last clicked, shown in the details area.
    private ItemData selectedItem;

    private bool isOpen;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged += HandleInventoryChanged;
        GameEvents.OnLanguageChanged += RefreshDetails;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= HandleInventoryChanged;
        GameEvents.OnLanguageChanged -= RefreshDetails;
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false); // Start closed.
        }
    }

    // Opens/closes the inventory when the Inventory button is pressed (polled).
    private void Update()
    {
        if (inputReader != null && inputReader.ToggleInventoryPressed)
        {
            Toggle();
        }
    }

    // Opens the inventory if it is closed, or closes it if it is open.
    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // Opens the inventory and freezes the game.
    public void Open()
    {
        // Only open during normal play, not during a cutscene or game over.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        isOpen = true;
        if (panel != null)
        {
            panel.SetActive(true);
        }

        selectedItem = null;
        RefreshSlots();
        RefreshDetails();

        if (GameManager.Instance != null && GameManager.Instance.PauseManager != null)
        {
            GameManager.Instance.PauseManager.Pause(this);
        }
    }

    // Closes the inventory and unfreezes the game.
    public void Close()
    {
        isOpen = false;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (GameManager.Instance != null && GameManager.Instance.PauseManager != null)
        {
            GameManager.Instance.PauseManager.Resume(this);
        }
    }

    // Remembers the new slots and redraws them.
    private void HandleInventoryChanged(IReadOnlyList<InventorySlot> slots)
    {
        currentSlots = slots;
        RefreshSlots();
    }

    // Rebuilds the grid of slots from the latest inventory slots.
    private void RefreshSlots()
    {
        ClearSlots();

        if (currentSlots == null || slotContainer == null || slotPrefab == null)
        {
            return;
        }

        foreach (InventorySlot slot in currentSlots)
        {
            InventorySlotUI slotView = Instantiate(slotPrefab, slotContainer);
            slotView.Show(slot, OnSlotClicked);
            spawnedSlots.Add(slotView);
        }
    }

    // Remembers which item was clicked and shows its details.
    private void OnSlotClicked(ItemData item)
    {
        selectedItem = item;
        RefreshDetails();
    }

    // Shows the selected item's name and description in the player's language.
    private void RefreshDetails()
    {
        if (nameText == null || descriptionText == null)
        {
            return;
        }

        LocalizationManager localization = GameManager.Instance != null ? GameManager.Instance.Localization : null;
        if (selectedItem == null || localization == null)
        {
            nameText.text = string.Empty;
            descriptionText.text = string.Empty;
            return;
        }

        nameText.text = localization.Get(selectedItem.DisplayName);
        descriptionText.text = localization.Get(selectedItem.Description);
    }

    // Destroys all the current slot views.
    private void ClearSlots()
    {
        foreach (InventorySlotUI slotView in spawnedSlots)
        {
            if (slotView != null)
            {
                Destroy(slotView.gameObject);
            }
        }
        spawnedSlots.Clear();
    }
}
