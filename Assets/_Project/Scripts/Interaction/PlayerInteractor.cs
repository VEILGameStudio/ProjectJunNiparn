// PlayerInteractor
// Lets the player use interactable objects in two ways, and sends both to the same code:
//   - Click: a ray goes from the camera through the mouse. The first click box it hits
//     on the Interactable layer is used (the nearest one, so no sorting is needed).
//   - Touch: pushing into an object's small solid collider (its Blocker, on the Blocking
//     layer), or walking into a trigger on the TouchZone layer. The big click box never
//     counts as a touch.
// Touching the same object again within the Touch Cooldown is ignored, and the cooldown
// starts again every frame the player is still touching it. So pushing or sliding along
// an object counts as one touch.
// Every successful interaction also resets the idle hint timer.
//
// Put this on: the Player root (the object with the CharacterController).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Interaction Camera (optional): leave empty to use the Main Camera.
//   - Touch Cooldown Seconds: how long the player must stop touching an object before it can be touched again.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Camera")]
    [Tooltip("The camera clicks are cast from. Leave empty to use the Main Camera.")]
    [SerializeField] private Camera interactionCamera;

    [Header("Touch")]
    [Tooltip("After touching an object, touching the SAME object again is ignored until the player has stopped touching it for this many seconds. Stops repeat triggers while pushing or sliding along it.")]
    [SerializeField] private float touchCooldownSeconds = 0.5f;

    private PlayerHealth health;
    private IInteractable lastTouched;
    private float lastTouchTime;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
        if (inputReader == null)
        {
            Debug.LogError($"PlayerInteractor on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
        }

        GameLayers.CheckExists(GameLayers.Blocking, this);
        GameLayers.CheckExists(GameLayers.Interactable, this);
        GameLayers.CheckExists(GameLayers.TouchZone, this);
    }

    // Checks for a click each frame (polling the shared Input Reader).
    private void Update()
    {
        if (inputReader != null && inputReader.InteractPressed)
        {
            HandleClick();
        }
    }

    // Runs when the player clicks. Uses the nearest interactable under the mouse.
    private void HandleClick()
    {
        if (interactionCamera == null || Mouse.current == null)
        {
            return;
        }

        // Ignore clicks that land on UI (like the inventory window).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        IInteractable target = FindInteractableUnderMouse();
        if (target != null && AllowsClick(target.Activation))
        {
            TryInteract(target);
        }
    }

    // Runs every frame the CharacterController pushes into a collider while moving.
    // Only solid colliders on the Blocking layer (an object's Blocker) count as a touch.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!GameLayers.IsOnLayer(hit.gameObject, GameLayers.Blocking))
        {
            return; // The floor and other objects are not touch points.
        }

        IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
        if (target != null)
        {
            TryTouch(target);
        }
    }

    // Runs when the player walks into a trigger. Only TouchZone triggers count as a touch.
    private void OnTriggerEnter(Collider other)
    {
        if (!GameLayers.IsOnLayer(other.gameObject, GameLayers.TouchZone))
        {
            return; // The big click boxes (Interactable layer) must never fire a touch.
        }

        IInteractable target = other.GetComponentInParent<IInteractable>();
        if (target != null)
        {
            TryTouch(target);
        }
    }

    // Uses a touched object if it allows Touch and was not still being touched a moment ago.
    private void TryTouch(IInteractable target)
    {
        if (!AllowsTouch(target.Activation))
        {
            return;
        }

        bool stillTouching = target == lastTouched && Time.time - lastTouchTime < touchCooldownSeconds;

        // Remembered on every contact, so pushing or sliding along the object counts as one touch.
        lastTouched = target;
        lastTouchTime = Time.time;

        if (!stillTouching)
        {
            TryInteract(target);
        }
    }

    // The one shared path for click and touch: check, interact, reset the hint timer.
    private void TryInteract(IInteractable target)
    {
        if (!IsGamePlaying())
        {
            return;
        }

        PlayerContext context = CreateContext();
        if (!target.CanInteract(context))
        {
            return;
        }

        target.Interact(context);

        if (GameManager.Instance != null && GameManager.Instance.HintManager != null)
        {
            GameManager.Instance.HintManager.NotifyInteraction();
        }
    }

    // Casts a ray from the camera through the mouse and returns the first interactable it hits.
    private IInteractable FindInteractableUnderMouse()
    {
        Ray ray = interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Click boxes are triggers, so the ray must be allowed to hit triggers.
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GameLayers.InteractableMask, QueryTriggerInteraction.Collide);
        if (!hitSomething)
        {
            return null;
        }

        return hit.collider.GetComponentInParent<IInteractable>();
    }

    // Bundles what an interactable may need to know about the player.
    private PlayerContext CreateContext()
    {
        Inventory inventory = null;
        if (GameManager.Instance != null)
        {
            inventory = GameManager.Instance.Inventory;
        }

        return new PlayerContext(transform, inventory, health);
    }

    // The player can only interact during normal gameplay.
    private bool IsGamePlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    private bool AllowsClick(ActivationMode mode)
    {
        return mode == ActivationMode.Click || mode == ActivationMode.Both;
    }

    private bool AllowsTouch(ActivationMode mode)
    {
        return mode == ActivationMode.Touch || mode == ActivationMode.Both;
    }
}
