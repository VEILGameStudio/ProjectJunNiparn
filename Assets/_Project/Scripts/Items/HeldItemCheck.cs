// HeldItemCheck
// Asks one question: is the player holding the right item? Use it inside a puzzle
// screen, with no code:
//   - Wire a UI Button's On Click to HeldItemCheck.Check().
//   - The player holds the Required Item  -> On Correct runs.
//   - Anything else (wrong item, empty hand) -> On Wrong runs.
// A puzzle may have several of these, one for each thing it asks for.
//
// USE UP ITEM - read this before ticking it:
//   The item is taken away only when the check succeeds. That is still not safe in a
//   puzzle with several steps: if step one uses up the item and the player then fails
//   step two, the save says the item is gone and the puzzle is not solved, and the
//   player is stuck for good with nothing on screen to explain it.
//   Tick it only on the LAST step of a puzzle, or when the puzzle can still be
//   finished without this item.
//
// From code, the same question is one line:
//   if (player.Inventory.IsHolding(keyItem)) { ... }
//
// Put this on: any GameObject of the puzzle, for example the puzzle's panel.
// Assign in Inspector: Required Item, On Correct, On Wrong.

using UnityEngine;
using UnityEngine.Events;

public class HeldItemCheck : MonoBehaviour
{
    [Header("What to check")]
    [Tooltip("The item the player must be holding (it must be in the hand slot of the inventory screen).")]
    [SerializeField] private ItemData requiredItem;

    [Tooltip("Tick to take one of the item away when the check succeeds. ONLY tick this on the LAST step of a puzzle, or when the puzzle can still be finished without this item. An item used up at step one of a puzzle that then fails at step two is gone from the save, and the player is stuck for good.")]
    [SerializeField] private bool useUpItem;

    [Header("What happens")]
    [Tooltip("Runs when the player is holding the Required Item. Wire the next step of the puzzle here. Put sounds in a sound field of a component, not here.")]
    [SerializeField] private UnityEvent onCorrect;

    [Tooltip("Runs when the player is holding something else, or nothing. Wire a 'wrong item' reaction here.")]
    [SerializeField] private UnityEvent onWrong;

    // Other scripts can listen to these as well.
    public UnityEvent OnCorrect => onCorrect;
    public UnityEvent OnWrong => onWrong;

    private void Start()
    {
        if (requiredItem == null)
        {
            Debug.LogError($"HeldItemCheck on '{name}': Required Item is not assigned. Drag the item the player must hold here.", this);
        }
    }

    // Checks the player's hand and runs On Correct or On Wrong. Wire a UI Button to this.
    public void Check()
    {
        Inventory inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inventory == null)
        {
            Debug.LogWarning($"HeldItemCheck on '{name}': no Inventory was found, so the check counts as wrong. Make sure GameSystems is in the scene.", this);
        }

        TryCheck(inventory);
    }

    // The same check for a script that already has the inventory, for example
    // TryCheck(player.Inventory) inside an interactable. Returns true when the item was right.
    public bool TryCheck(Inventory inventory)
    {
        if (requiredItem == null || inventory == null || !inventory.IsHolding(requiredItem))
        {
            onWrong?.Invoke();
            return false;
        }

        // Take the item first, so anything On Correct saves already has it gone.
        if (useUpItem)
        {
            inventory.Remove(requiredItem, 1);
        }

        onCorrect?.Invoke();
        return true;
    }
}
