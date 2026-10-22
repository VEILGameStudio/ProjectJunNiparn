// InteractableBase
// The shared base for every interactable object. It does all the common work, so
// each interactable script only has to say what happens when it is used:
//   - Activation: Click, Touch, or Both.
//   - Max Interact Distance: how close the player must be, measured to the nearest
//     point of the click box.
//   - One Shot: the object can be used only once, then switches itself off.
//   - Unique Id: the name the save file remembers a one-time object by, so it stays
//     used after loading. Needed when One Shot is ticked, on every pickup, and on
//     every container that holds something.
//   - Required Item: the player must carry this item, otherwise the Blocked
//     Message is shown instead.
//   - Must Be Held: carrying the Required Item is not enough, the player must be
//     holding it (hand slot of the inventory screen). A player who has it in the bag
//     but is not holding it sees the Not Held Message, so they know what to do.
//   - Interact Sound: an optional sound played on every use.
//
// How to make a new interactable:
//   public class LeverInteractable : InteractableBase
//   {
//       protected override void OnInteract(PlayerContext player) { ... }
//   }
// IMPORTANT: never write Start, OnEnable or OnDisable in your own interactable. Unity
// would then skip the ones in this script, and saving stops working without any error
// at that moment. Override OnStarted, OnEnabled or OnDisabled instead:
//       protected override void OnStarted() { ... }
//
// Put this on: the root of the object, built like this (each collider on its own
// GameObject, because a layer belongs to a whole GameObject):
//   Chest              layer Interactable, BoxCollider with Is Trigger, a box around the visible sprite or mesh (the mouse clicks this)
//     └─ Blocker       layer Blocking, small solid BoxCollider at the base (the player bumps into this)
//     └─ TouchZone     layer TouchZone, trigger (optional: only for walk-into areas)
// Use only the parts you need: click-only objects need just the click box;
// objects touched by bumping need the Blocker; walk-into areas need a TouchZone.
// Assign in Inspector: the fields below (all have working defaults).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Tooltip("Tick if this can be used only once. It switches itself (and its highlight) off afterwards, and the save file remembers it. Pickups, and containers that hold something, are always used only once, so they do not show this box.")]
    [SerializeField] private bool oneShot;

    [Tooltip("Needed for anything that is used only once: One Shot ticked, every pickup, and every container that holds something. A short name that no other object in this scene has, for example 'corridor_key'. The save file uses it to remember that this was already used, so it does not come back after loading.")]
    [SerializeField] private string uniqueId;

    [Header("Click Area (optional)")]
    [Tooltip("The click box: the trigger collider on the Interactable layer that the mouse clicks. Leave empty to find it automatically.")]
    [SerializeField] private Collider clickArea;

    [Header("Requirement (optional)")]
    [Tooltip("The item the player must carry to use this. Leave empty for no requirement.")]
    [SerializeField] private ItemData requiredItem;

    [Tooltip("Shown when the player does not carry the Required Item, e.g. 'It is locked. I need a key.'")]
    [SerializeField] private LocalizedString blockedMessage;

    [Tooltip("Tick if carrying the Required Item is not enough: the player must be HOLDING it (it must be in the hand slot of the inventory screen).")]
    [SerializeField] private bool mustBeHeld;

    [Tooltip("Only used when Must Be Held is ticked. Shown when the player has the Required Item in the bag but is not holding it, e.g. 'I need to hold the key.' Drag a Mini dialogue asset here. Leave empty to show the Blocked Message instead.")]
    [SerializeField] private MiniDialogueData notHeldMessage;

    [Header("Feedback (optional)")]
    [Tooltip("A sound played every time this is used.")]
    [SerializeField] private AudioClip interactSound;

    // The classes already reported for writing their own Start, so each is reported once.
    private static readonly HashSet<System.Type> reportedClasses = new HashSet<System.Type>();

    private Collider distanceCollider;

    // True once this was used up, either just now or in a save that was loaded.
    private bool isUsed;

    // True once the Start of this script has run.
    private bool baseStarted;

    // How this is started (from IInteractable).
    public ActivationMode Activation => activation;

    // True if this can be used only once. Subclasses can read it.
    protected bool OneShot => oneShot;

    // True when the save file must remember this after it was used. A script that is always
    // used only once (a pickup, a container that holds something) overrides this.
    protected virtual bool UsedOnlyOnce => oneShot;

    // The name this object is saved under. The scene name is part of it, so a Unique Id only
    // has to be different from the others in the same scene.
    private string SavedId => gameObject.scene.name + "/" + uniqueId;

    // Do not write Start, OnEnable or OnDisable in a subclass (see the top of this file).
    private void Start()
    {
        baseStarted = true;
        RestoreIfUsed();
        OnStarted();
    }

    private void OnEnable()
    {
        GameEvents.OnSceneReady += RestoreIfUsed;
        OnEnabled();
    }

    private void OnDisable()
    {
        GameEvents.OnSceneReady -= RestoreIfUsed;
        OnDisabled();
    }

    // Runs once when the object starts. Override this instead of writing Start.
    protected virtual void OnStarted()
    {
    }

    // Runs every time the object is switched on. Override this instead of writing OnEnable.
    protected virtual void OnEnabled()
    {
    }

    // Runs every time the object is switched off. Override this instead of writing OnDisable.
    protected virtual void OnDisabled()
    {
    }

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
        ReportIfStartWasReplaced();

        Inventory inventory = player != null ? player.Inventory : null;
        RequiredItemResult itemCheck = CheckRequiredItem(inventory, requiredItem, mustBeHeld);
        if (itemCheck == RequiredItemResult.Missing)
        {
            ShowBlockedMessage();
            return;
        }
        if (itemCheck == RequiredItemResult.NotHeld)
        {
            ShowNotHeldMessage();
            return;
        }

        if (interactSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(interactSound);
        }

        OnInteract(player);

        if (oneShot)
        {
            MarkUsed();
        }
    }

    // What happens when the player uses this. Every interactable writes its own.
    protected abstract void OnInteract(PlayerContext player);

    // Uses this up: the save file remembers it, and it switches itself off. A script that is
    // always used only once (a pickup, a container) calls this itself.
    protected void MarkUsed()
    {
        if (isUsed)
        {
            return;
        }
        isUsed = true;

        if (SaveManager.Instance != null && !string.IsNullOrEmpty(uniqueId))
        {
            SaveManager.Instance.MarkOneShotUsed(SavedId);
        }

        SwitchOff();
    }

    // Puts this into its "already used" state without using it. The save system does this
    // after loading. Calling it again does nothing, so it is safe to call twice.
    public void RestoreAsUsed()
    {
        if (isUsed)
        {
            return;
        }
        isUsed = true;

        OnRestoredAsUsed();
    }

    // What "already used" looks like for this object. It must only SET things (never switch
    // something back and forth), give nothing, and play no sound or message.
    protected virtual void OnRestoredAsUsed()
    {
        SwitchOff();
    }

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

    // Shows a short line from a dialogue asset in the mini dialogue popup. New message
    // fields use this one: the text lives in the asset, not typed into the object.
    protected void ShowMiniMessage(MiniDialogueData message)
    {
        if (message == null)
        {
            return;
        }
        if (UIManager.Instance == null || UIManager.Instance.Dialogue == null)
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': no DialogueManager was found. Make sure the PersistentCanvas (with a UIManager) is in the scene.", this);
            return;
        }

        UIManager.Instance.Dialogue.ShowMini(message);
    }

    // Finds the one-time objects in a scene whose Unique Id is empty, or shared with another
    // one. Objects that are switched off are checked too. Returns one message per problem.
    // Public and static so the SaveManager can run it once per scene, and so tests can check it.
    public static List<string> FindIdProblems(Scene scene)
    {
        List<string> problems = new List<string>();
        Dictionary<string, InteractableBase> owners = new Dictionary<string, InteractableBase>();

        foreach (InteractableBase interactable in FindObjectsByType<InteractableBase>(FindObjectsInactive.Include))
        {
            if (interactable.gameObject.scene != scene || !interactable.UsedOnlyOnce)
            {
                continue;
            }

            string scriptName = interactable.GetType().Name;
            if (string.IsNullOrEmpty(interactable.uniqueId))
            {
                problems.Add($"{scriptName} on '{interactable.name}': Unique Id is empty, so the game cannot remember that this was used and it comes back after loading a save. Type a short name that no other object in this scene has, for example 'corridor_key'.");
                continue;
            }

            if (owners.TryGetValue(interactable.uniqueId, out InteractableBase firstOwner))
            {
                problems.Add($"{scriptName} on '{interactable.name}': Unique Id '{interactable.uniqueId}' is also used by '{firstOwner.name}'. Using one of them would switch the other off after loading a save. Give each of them its own Unique Id.");
                continue;
            }

            owners.Add(interactable.uniqueId, interactable);
        }

        return problems;
    }

    // Switches this off if the save says it was already used. Runs when the object starts and
    // when a scene is ready; an object inside a camera zone that was closed only starts later.
    private void RestoreIfUsed()
    {
        if (isUsed || !UsedOnlyOnce || string.IsNullOrEmpty(uniqueId) || SaveManager.Instance == null)
        {
            return;
        }

        if (SaveManager.Instance.IsOneShotUsed(SavedId))
        {
            RestoreAsUsed();
        }
    }

    // A subclass that writes its own Start stops the Start of this script from running, and
    // then saving does not work. Says so once per class, the first time one is used.
    private void ReportIfStartWasReplaced()
    {
        if (baseStarted || !Application.isPlaying || !reportedClasses.Add(GetType()))
        {
            return;
        }

        Debug.LogError($"{GetType().Name} on '{name}': this script has its own Start method, so the Start of InteractableBase never ran and the game cannot remember when this was used. In {GetType().Name}, rename that method to 'protected override void OnStarted()'.", this);
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

    // Decides whether a player with this inventory may use something that needs an item.
    // "Does not have it" and "has it but is not holding it" are different answers, because
    // the player needs a different message for each. Public and static so tests can check it.
    public static RequiredItemResult CheckRequiredItem(Inventory inventory, ItemData requiredItem, bool mustBeHeld)
    {
        if (requiredItem == null)
        {
            return RequiredItemResult.Ready;
        }
        if (inventory == null || !inventory.Has(requiredItem))
        {
            return RequiredItemResult.Missing;
        }
        if (mustBeHeld && !inventory.IsHolding(requiredItem))
        {
            return RequiredItemResult.NotHeld;
        }

        return RequiredItemResult.Ready;
    }

    // Tells the player they have the item but must hold it. Falls back to the Blocked
    // Message when no Not Held Message was set.
    private void ShowNotHeldMessage()
    {
        if (notHeldMessage == null)
        {
            ShowBlockedMessage();
            return;
        }

        ShowMiniMessage(notHeldMessage);
    }

    // Tells the player why this cannot be used yet. Always the small mini popup, never a full dialogue.
    private void ShowBlockedMessage()
    {
        if (blockedMessage == null || (string.IsNullOrEmpty(blockedMessage.english) && string.IsNullOrEmpty(blockedMessage.thai)))
        {
            Debug.LogWarning($"{GetType().Name} on '{name}': the player does not have the Required Item, but Blocked Message is empty. Type a message so the player knows why.", this);
            return;
        }

        ShowMiniMessage(blockedMessage);
    }

    // Turns this interactable and its highlight off. It only sets things, so it is safe to run twice.
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
