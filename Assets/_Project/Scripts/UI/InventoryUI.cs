// InventoryUI
// The inventory screen: the hand slot on the left and the bag on the right.
//   - Open or close it with the bag button on screen, or the Inventory key (I or Tab).
//     The game freezes while it is open.
//   - Click an item in the bag to hold it. Click the hand slot to put it away.
//   - The bag always draws at least Minimum Cells cells, and adds whole rows when the
//     player carries more. That number is not a limit: the bag never refuses an item.
//   - The bag shows one fewer of the item that is held, because that one is in the hand.
// This screen never changes the inventory itself. It only asks the Inventory to Hold or
// StopHolding, and redraws when GameEvents says something changed.
//
// Put this on: an "InventoryUI" GameObject under the PersistentCanvas that is ALWAYS
//   active. Do NOT put it on the Panel it shows and hides, or the Inventory key
//   stops working once the panel is hidden.
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Panel: the inventory window GameObject to show/hide.
//   - Open Button: the bag button at the bottom right of the screen.
//   - Close Button: the X button inside the window (optional).
//   - Hand Slot: the big slot on the left of the window (has an InventorySlotUI).
//   - Slot Container: the parent (with a Grid Layout Group) that the bag cells go into.
//   - Slot Prefab: the Inventory Slot prefab (has an InventorySlotUI).
//   - Name Text / Description Text: TextMeshPro texts for the held item.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Windows")]
    [Tooltip("The inventory window that is shown or hidden.")]
    [SerializeField] private GameObject panel;

    [Tooltip("The bag button on screen (bottom right). Clicking it opens or closes the inventory.")]
    [SerializeField] private Button openButton;

    [Tooltip("The close button inside the inventory window. Optional.")]
    [SerializeField] private Button closeButton;

    [Header("Hand")]
    [Tooltip("The slot that shows the item the player is holding. Clicking it puts the item back in the bag.")]
    [SerializeField] private InventorySlotUI handSlot;

    [Header("Bag")]
    [Tooltip("The parent object (with a Grid Layout Group) that the bag cells are placed in.")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("The Inventory Slot prefab used for each cell of the bag.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Tooltip("How many cells are in one row. Keep it the same as the Constraint Count of the Grid Layout Group.")]
    [Min(1)]
    [SerializeField] private int columns = 5;

    [Tooltip("The bag always draws at least this many cells, even when it is empty. This is NOT a limit: when the player carries more, the bag adds rows and scrolls.")]
    [Min(0)]
    [SerializeField] private int minimumCells = 25;

    [Header("Held Item Details")]
    [Tooltip("TextMeshPro text that shows the held item's name.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("TextMeshPro text that shows the held item's description.")]
    [SerializeField] private TMP_Text descriptionText;

    // The cells of the bag. They are made once and reused every time the bag is redrawn.
    private readonly List<InventorySlotUI> spawnedSlots = new List<InventorySlotUI>();

    // The latest slots sent by GameEvents.OnInventoryChanged.
    private IReadOnlyList<InventorySlot> currentSlots;

    // The latest held item sent by GameEvents.OnHeldItemChanged.
    private ItemData heldItem;

    private bool isOpen;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged += HandleInventoryChanged;
        GameEvents.OnHeldItemChanged += HandleHeldItemChanged;
        GameEvents.OnLanguageChanged += RefreshDetails;

        if (openButton != null)
        {
            openButton.onClick.AddListener(Toggle);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= HandleInventoryChanged;
        GameEvents.OnHeldItemChanged -= HandleHeldItemChanged;
        GameEvents.OnLanguageChanged -= RefreshDetails;

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Toggle);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    private void Start()
    {
        if (panel == null)
        {
            Debug.LogError("InventoryUI on 'InventoryUI': Panel is not assigned. Drag the InventoryPanel object here.", this);
        }
        if (handSlot == null)
        {
            Debug.LogError("InventoryUI on 'InventoryUI': Hand Slot is not assigned. Drag the HandSlot object (the big slot on the left of the inventory window) here.", this);
        }
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("InventoryUI on 'InventoryUI': Slot Container or Slot Prefab is not assigned. Drag the SlotContainer object and the SlotPrefab prefab here.", this);
        }

        if (panel != null)
        {
            panel.SetActive(false); // Start closed.
        }
    }

    // Opens/closes the inventory when the Inventory key is pressed (polled).
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
        if (isOpen || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
        {
            return;
        }

        isOpen = true;
        if (panel != null)
        {
            panel.SetActive(true);
        }

        RefreshAll();

        if (GameManager.Instance != null && GameManager.Instance.PauseManager != null)
        {
            GameManager.Instance.PauseManager.Pause(this);
        }
    }

    // Closes the inventory and unfreezes the game.
    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

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

    // The stacks the bag draws: everything the player carries, minus the one item that is
    // in the hand. One is taken from the LAST stack of the held item, and a stack that
    // reaches 0 is left out. If the held item is not in any stack, every stack is returned
    // as it is. Returns copies, so the real inventory is never changed here.
    public static List<InventorySlot> BagStacksWithoutHeld(IReadOnlyList<InventorySlot> stacks, ItemData heldItem)
    {
        List<InventorySlot> bagStacks = new List<InventorySlot>();
        if (stacks == null)
        {
            return bagStacks;
        }

        // Find the last stack of the held item. It stays -1 when nothing matches.
        int heldIndex = -1;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (heldItem != null && stacks[i].Item == heldItem)
            {
                heldIndex = i;
            }
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            int count = stacks[i].Count;
            if (i == heldIndex)
            {
                count -= 1;
            }
            if (count > 0)
            {
                bagStacks.Add(new InventorySlot(stacks[i].Item, count));
            }
        }

        return bagStacks;
    }

    // How many cells the bag draws: never fewer than the minimum, and always whole rows.
    public static int CountCells(int stackCount, int columns, int minimumCells)
    {
        int cellsPerRow = Mathf.Max(1, columns);
        int cells = Mathf.Max(stackCount, minimumCells);
        int rows = Mathf.CeilToInt(cells / (float)cellsPerRow);
        return rows * cellsPerRow;
    }

    // Remembers the new slots and redraws the bag.
    private void HandleInventoryChanged(IReadOnlyList<InventorySlot> slots)
    {
        currentSlots = slots;
        RefreshAll();
    }

    // Remembers the new held item and redraws the hand and the bag.
    private void HandleHeldItemChanged(ItemData newHeldItem)
    {
        heldItem = newHeldItem;
        RefreshAll();
    }

    // Redraws everything, but only while the window is open.
    private void RefreshAll()
    {
        if (!isOpen)
        {
            return;
        }

        RefreshHand();
        RefreshSlots();
        RefreshDetails();
    }

    // Shows the held item in the hand slot, or an empty hand slot.
    private void RefreshHand()
    {
        if (handSlot == null)
        {
            return;
        }

        if (heldItem == null)
        {
            handSlot.ShowEmpty();
        }
        else
        {
            handSlot.Show(new InventorySlot(heldItem, 1), HandleHandClicked);
        }
    }

    // Fills the bag cells from the latest inventory slots, reusing the cells that exist.
    private void RefreshSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            return;
        }

        List<InventorySlot> bagStacks = BagStacksWithoutHeld(currentSlots, heldItem);
        int cellCount = CountCells(bagStacks.Count, columns, minimumCells);

        while (spawnedSlots.Count < cellCount)
        {
            spawnedSlots.Add(Instantiate(slotPrefab, slotContainer));
        }

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            InventorySlotUI cell = spawnedSlots[i];
            cell.gameObject.SetActive(i < cellCount);

            if (i < bagStacks.Count)
            {
                cell.Show(bagStacks[i], HandleBagItemClicked);
            }
            else
            {
                cell.ShowEmpty();
            }
        }
    }

    // An item in the bag was clicked: ask the Inventory to hold it.
    private void HandleBagItemClicked(ItemData item)
    {
        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
        {
            GameManager.Instance.Inventory.Hold(item);
        }
    }

    // The hand slot was clicked: ask the Inventory to put the item away.
    private void HandleHandClicked(ItemData item)
    {
        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
        {
            GameManager.Instance.Inventory.StopHolding();
        }
    }

    // Shows the held item's name and description in the player's language.
    private void RefreshDetails()
    {
        if (nameText == null || descriptionText == null)
        {
            return;
        }

        LocalizationManager localization = GameManager.Instance != null ? GameManager.Instance.Localization : null;
        if (heldItem == null || localization == null)
        {
            nameText.text = string.Empty;
            descriptionText.text = string.Empty;
            return;
        }

        nameText.text = localization.Get(heldItem.DisplayName);
        descriptionText.text = localization.Get(heldItem.Description);
    }
}
