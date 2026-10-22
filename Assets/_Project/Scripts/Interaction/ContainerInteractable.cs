// ContainerInteractable
// A chest, crate, drawer or bookshelf the player opens. Every container in the game is
// this one script. Two containers differ only in what is put in the fields:
//   - Contents left empty -> it shows the Empty Message, every time. Nothing else happens.
//   - Contents filled     -> it gives the item, shows the Taken Message, runs On Opened,
//                            and is then used up: it cannot be opened again, also after
//                            loading a save.
// Need something extra to happen when it opens (a door opens, a lid animation plays)?
// Wire it into On Opened in the Inspector. Do not write a new script for that.
// Two rules for On Opened:
//   - No sounds. On Opened also runs when a save is loaded where this was already opened,
//     so a sound in it would play on every load. Put the sound in Interact Sound instead.
//   - Only things that SET something (Open the door, Show the open lid). Never something
//     that switches back and forth.
//
// Put this on: the container GameObject. Add a BoxCollider click box first
//   (Is Trigger ticked, layer Interactable) - see InteractableBase for the full setup.
// Assign in Inspector:
//   - Contents: the item inside. Leave empty for an empty container.
//   - Amount: how many of the item the player gets.
//   - Empty Message: the Mini Dialogue asset shown when Contents is empty.
//   - Taken Message: the Mini Dialogue asset shown when the item is taken.
//     (Messages are assets from Data/Dialogue, made from the writer's JSON. Text is never
//     typed into this object.)
//   - Unique Id (from InteractableBase): needed when Contents is filled. A short name no
//     other object in this scene has, for example 'room_chest'.
//   - On Opened (optional): what else happens when the item is taken.
//   - Activation / Max Interact Distance / Interact Sound / Required Item: see InteractableBase.

using UnityEngine;
using UnityEngine.Events;

public class ContainerInteractable : InteractableBase
{
    [Header("Contents")]
    [Tooltip("The item inside. Leave empty for an empty container.")]
    [SerializeField] private ItemData contents;

    [Tooltip("How many of the item the player gets.")]
    [Min(1)]
    [SerializeField] private int amount = 1;

    [Header("Messages (Mini Dialogue assets from Data/Dialogue)")]
    [Tooltip("Shown when Contents is empty, for example 'Empty.'")]
    [SerializeField] private MiniDialogueData emptyMessage;

    [Tooltip("Shown when the player takes the item, for example 'Got the key.'")]
    [SerializeField] private MiniDialogueData takenMessage;

    [Header("Events")]
    [Tooltip("Runs when the item is taken. It also runs, silently, when a save is loaded where this was already opened, so a door it opened is open again. Do NOT put sounds here (use Interact Sound), and only wire things that set something, like Open. Never something that switches back and forth.")]
    [SerializeField] private UnityEvent onOpened;

    // Runs when the item is taken. Scripts can listen to it too.
    public UnityEvent OnOpened => onOpened;

    // A container that holds something is always used only once, so the save file must remember it.
    protected override bool UsedOnlyOnce => contents != null;

    // Says so in the Console when the message this container will need is missing.
    protected override void OnStarted()
    {
        if (contents == null && emptyMessage == null)
        {
            Debug.LogError($"ContainerInteractable on '{name}': Contents is empty, but Empty Message is not assigned, so nothing happens when the player opens it. Drag a Mini Dialogue asset (for example 'box_empty' from Data/Dialogue) into Empty Message.", this);
        }
        if (contents != null && takenMessage == null)
        {
            Debug.LogError($"ContainerInteractable on '{name}': it holds '{contents.name}', but Taken Message is not assigned, so the player is not told what they got. Drag a Mini Dialogue asset from Data/Dialogue into Taken Message.", this);
        }
    }

    // Empty: says so. Otherwise: gives the item, says so, and is used up.
    protected override void OnInteract(PlayerContext player)
    {
        if (contents == null)
        {
            ShowMiniMessage(emptyMessage);
            return;
        }
        if (player.Inventory == null)
        {
            Debug.LogError($"ContainerInteractable on '{name}': no Inventory was found. Make sure the Managers object (with an Inventory) is in the scene.", this);
            return;
        }

        player.Inventory.Add(contents, amount);
        MarkUsed(); // Before the pickup event, so the autosave it starts already knows this is open.
        GameEvents.RaiseItemPickedUp(contents, amount);

        ShowMiniMessage(takenMessage);
        onOpened?.Invoke();
    }

    // In a loaded save where this was already opened: it stays shut for use, and whatever it
    // opened is opened again. It gives nothing and shows no message.
    protected override void OnRestoredAsUsed()
    {
        base.OnRestoredAsUsed();
        onOpened?.Invoke();
    }
}
