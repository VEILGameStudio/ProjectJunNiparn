// InteractableBase
// The shared base for every interactable object. It does all the common work, so
// each interactable script only has to say what happens when it is used:
//   - Activation: Click, Touch, or Both.
//   - Max Interact Distance: how close the player must be, measured to the nearest
//     point of the click box.
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
// Put this on: the root of the object, built like this (each collider on its own
// GameObject, because a layer belongs to a whole GameObject):
//   Chest              layer Interactable, BoxCollider with Is Trigger, a box around the visible sprite or mesh (the mouse clicks this)
//     └─ Blocker       layer Blocking, small solid BoxCollider at the base (the player bumps into this)
//     └─ TouchZone     layer TouchZone, trigger (optional: only for walk-into areas)
// Use only the parts you need: click-only objects need just the click box;
// objects touched by bumping need the Blocker; walk-into areas need a TouchZone.
// Assign in Inspector: the fields below (all have working defaults).

using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Activation")]
    [Tooltip("Click = mouse click on the click box. Touch = the player bumps its Blocker or walks into its TouchZone. Both = either one.")]
    [SerializeField] private ActivationMode activation = ActivationMode.Click;

    [Tooltip("How close (in units) the player must be to use this, measured to the nearest point of the click box.")]
    [FormerlySerializedAs("maxClickDistance")]
    [SerializeField] private float maxInteractDistance = 2f;

    [Tooltip("Tick if this can be used only once. It switches itself (and its highlight) off afterwards.")]
    [SerializeField] private bool oneShot;

    [Header("Click Area (optional)")]
    [Tooltip("The click box: the trigger collider on the Interactable layer that the mouse clicks. Leave empty to find it automatically.")]
    [SerializeField] private Collider clickArea;

    [Header("Requirement (optional)")]
    [Tooltip("The item the player must carry to use this. Leave empty for no requirement.")]
    [SerializeField] private ItemData requiredItem;

    [Tooltip("Shown when the player does not carry the Required Item, e.g. 'It is locked. I need a key.'")]
    [SerializeField] private LocalizedString blockedMessage;

    [Header("Feedback (optional)")]
    [Tooltip("A sound played every time this is used.")]
    [SerializeField] private AudioClip interactSound;

    private Collider distanceCollider;

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

        Vector3 playerPosition = player.Transform.position;
        Vector3 nearestPoint = GetDistanceCollider().ClosestPoint(playerPosition);
        return Vector3.Distance(playerPosition, nearestPoint) <= maxInteractDistance;
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

    // The collider distance is measured to: the click box, or else this object's own collider.
    private Collider GetDistanceCollider()
    {
        if (distanceCollider == null)
        {
            distanceCollider = clickArea != null ? clickArea : FindClickArea();
        }
        if (distanceCollider == null)
        {
            distanceCollider = GetComponent<Collider>();
        }
        return distanceCollider;
    }

    // Finds the trigger on the Interactable layer, on this object or its children.
    private Collider FindClickArea()
    {
        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            if (candidate.isTrigger && GameLayers.IsOnLayer(candidate.gameObject, GameLayers.Interactable))
            {
                return candidate;
            }
        }
        return null;
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
        // The highlight usually sits on the Visual child, next to the SpriteRenderer.
        HighlightEffect highlight = GetComponentInChildren<HighlightEffect>();
        if (highlight != null)
        {
            highlight.enabled = false;
        }

        enabled = false;
    }

    // Warns in the Editor while the object is being set up, if its colliders cannot work.
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        bool usesClick = activation == ActivationMode.Click || activation == ActivationMode.Both;
        bool usesTouch = activation == ActivationMode.Touch || activation == ActivationMode.Both;

        if (clickArea != null && (!clickArea.isTrigger || !GameLayers.IsOnLayer(clickArea.gameObject, GameLayers.Interactable)))
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': Click Area must be a trigger (tick Is Trigger) on the '{GameLayers.Interactable}' layer.", this);
        }
        if (usesClick && clickArea == null && FindClickArea() == null)
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': Activation includes Click, but there is no click box. Add a BoxCollider with Is Trigger ticked around the visible sprite or mesh, on the '{GameLayers.Interactable}' layer.", this);
        }
        if (usesTouch && !HasTouchCollider())
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': Activation includes Touch, but nothing can be touched. Add a small solid collider at the base (a child on the '{GameLayers.Blocking}' layer) to touch by bumping, or a trigger on the '{GameLayers.TouchZone}' layer to touch by walking in. The click box never counts as a touch.", this);
        }
    }

    // True if this object has something the player can touch: a solid collider on Blocking, or a TouchZone trigger.
    private bool HasTouchCollider()
    {
        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            bool isBlocker = !candidate.isTrigger && GameLayers.IsOnLayer(candidate.gameObject, GameLayers.Blocking);
            bool isTouchZone = candidate.isTrigger && GameLayers.IsOnLayer(candidate.gameObject, GameLayers.TouchZone);
            if (isBlocker || isTouchZone)
            {
                return true;
            }
        }
        return false;
    }
}
