// InventoryUI
// Shows the player's bag. Press the Inventory button to open or close it (this
// also freezes the game while open). It draws one slot per item using the slot
// prefab, and clicking a slot shows that item's name and description. It only
// READS the inventory and listens to its OnInventoryChanged event - it never
// changes the inventory data itself.
//
// Put this on: an "InventoryUI" GameObject under your main Canvas.
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

    // The item the player last clicked, shown in the details area.
    private ItemData selectedItem;

    private bool isOpen;

    private void OnEnable()
    {
        GameEvents.LanguageChanged += RefreshDetails;
    }

    private void OnDisable()
    {
        GameEvents.LanguageChanged -= RefreshDetails;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= RefreshSlots;
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

    private void Start()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += RefreshSlots;
        }
        else
        {
            Debug.LogError("InventoryUI: no Inventory was found. Make sure the Managers prefab with the Inventory is in the scene.", this);
        }

        if (panel != null)
        {
            panel.SetActive(false); // Start closed.
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

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.Pause(this);
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

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.Resume(this);
        }
    }

    // Rebuilds the grid of slots from the current inventory.
    private void RefreshSlots()
    {
        if (Inventory.Instance == null || slotContainer == null || slotPrefab == null)
        {
            return;
        }

        ClearSlots();

        foreach (InventorySlot slot in Inventory.Instance.Slots)
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

        if (selectedItem == null || LocalizationManager.Instance == null)
        {
            nameText.text = string.Empty;
            descriptionText.text = string.Empty;
            return;
        }

        nameText.text = LocalizationManager.Instance.Get(selectedItem.DisplayName);
        descriptionText.text = LocalizationManager.Instance.Get(selectedItem.Description);
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
