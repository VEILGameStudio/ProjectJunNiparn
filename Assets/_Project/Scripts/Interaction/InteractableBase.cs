// InteractableBase
// The shared base for every interactable object. It does all the common work, so
// each interactable script only has to say what happens when it is used:
//   - Activation: Click, Touch, or Both.
//   - Max Click Distance: how close the player must be.
//   - One Shot: the object can be used only once, then switches itself off.
//   - Required Item: the player must carry this item, otherwise the Blocked
//     Message is shown instead.
//   - Interact Sound: an optional sound played on every use.
//
// How to make a new interactable:
//   public class LeverInteractable : InteractableBase
//   {
//       protected override void OnInteract(PlayerContext player) { ... }
//   }
//
// Put this on: nothing directly. Put a subclass (Door, PickupItem, ...) on an object
//   that has a Collider2D. Add the Collider2D first (for example a BoxCollider2D),
//   and tick "Is Trigger" on it if the object uses Touch.
// Assign in Inspector: the fields below (all have working defaults).

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Activation")]
    [Tooltip("Click = mouse click. Touch = the player walks into it. Both = either one.")]
    [SerializeField] private ActivationMode activation = ActivationMode.Click;

    [Tooltip("How close (in units) the player must be to use this, measured to the nearest edge of the collider.")]
    [SerializeField] private float maxClickDistance = 2f;

    [Tooltip("Tick if this can be used only once. It switches itself (and its highlight) off afterwards.")]
    [SerializeField] private bool oneShot;

    [Header("Requirement (optional)")]
    [Tooltip("The item the player must carry to use this. Leave empty for no requirement.")]
    [SerializeField] private ItemData requiredItem;

    [Tooltip("Shown when the player does not carry the Required Item, e.g. 'It is locked. I need a key.'")]
    [SerializeField] private LocalizedString blockedMessage;

    [Header("Feedback (optional)")]
    [Tooltip("A sound played every time this is used.")]
    [SerializeField] private AudioClip interactSound;

    private Collider2D ownCollider;

    // How this is started (from IInteractable).
    public ActivationMode Activation => activation;

    // True if this can be used only once. Subclasses can read it.
    protected bool OneShot => oneShot;

    // True when this is switched on and the player is close enough.
    public virtual bool CanInteract(PlayerContext player)
    {
        if (!isActiveAndEnabled || player == null || player.Transform == null)
        {
            return false;
        }

        if (ownCollider == null)
        {
            ownCollider = GetComponent<Collider2D>();
        }

        // Distance to the nearest edge, so a player standing inside a trigger counts as 0.
        Vector2 playerPosition = player.Transform.position;
        Vector2 nearestPoint = ownCollider.ClosestPoint(playerPosition);
        return Vector2.Distance(playerPosition, nearestPoint) <= maxClickDistance;
    }

    // Checks the Required Item, plays the sound, runs OnInteract, then handles One Shot.
    public void Interact(PlayerContext player)
    {
        if (!HasRequiredItem(player))
        {
            ShowBlockedMessage();
            return;
        }

        if (interactSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(interactSound);
        }

        OnInteract(player);

        if (oneShot)
        {
            SwitchOff();
        }
    }

    // What happens when the player uses this. Every interactable writes its own.
    protected abstract void OnInteract(PlayerContext player);

    // Shows a short line in the mini dialogue popup. Subclasses can use this too.
    protected void ShowMiniMessage(LocalizedString message)
    {
        if (UIManager.Instance == null || UIManager.Instance.MiniDialogue == null)
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': no MiniDialogueUI was found. Make sure the PersistentCanvas (with a UIManager) is in the scene.", this);
            return;
        }

        UIManager.Instance.MiniDialogue.ShowLocalized(message);
    }

    // True if no item is required, or the player carries it.
    private bool HasRequiredItem(PlayerContext player)
    {
        if (requiredItem == null)
        {
            return true;
        }

        return player.Inventory != null && player.Inventory.Has(requiredItem);
    }

    // Tells the player why this cannot be used yet.
    private void ShowBlockedMessage()
    {
        if (blockedMessage == null || (string.IsNullOrEmpty(blockedMessage.english) && string.IsNullOrEmpty(blockedMessage.thai)))
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': the player does not have the Required Item, but Blocked Message is empty. Type a message so the player knows why.", this);
            return;
        }

        ShowMiniMessage(blockedMessage);
    }

    // Turns this interactable and its highlight off after a one-shot use.
    private void SwitchOff()
    {
        HighlightEffect highlight = GetComponent<HighlightEffect>();
        if (highlight != null)
        {
            highlight.enabled = false;
        }

        enabled = false;
    }
}
