// PlayerInteractor
// Lets the player use interactable objects, in two ways:
//   - Click: clicking an object with the mouse (its Activation is Click or Both).
//   - Touch: walking into an object's trigger (its Activation is Touch or Both).
// Both ways go through the same TryInteract method, so the checks are identical.
// Every successful interaction also resets the idle hint timer.
//
// Put this on: the Player GameObject (the one with the Rigidbody2D and Collider2D,
//   so walking into triggers is detected).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Interaction Camera (optional): leave empty to use the Main Camera.
//   - Interactable Layers: which layers can be clicked (leave as Everything to start).

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Camera")]
    [Tooltip("The camera clicks are measured from. Leave empty to use the Main Camera.")]
    [SerializeField] private Camera interactionCamera;

    [Header("Click")]
    [Tooltip("Which layers can be clicked on. Leave as Everything to start.")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    private PlayerHealth health;

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
    }

    // Checks for a click each frame (polling the shared Input Reader).
    private void Update()
    {
        if (inputReader != null && inputReader.InteractPressed)
        {
            HandleClick();
        }
    }

    // Runs when the player clicks. Finds the interactable under the mouse and uses it.
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

        Vector2 worldPoint = interactionCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        IInteractable target = FindInteractableAt(worldPoint);
        if (target != null && AllowsClick(target.Activation))
        {
            TryInteract(target);
        }
    }

    // Runs when the player walks into a trigger. Uses it if it allows Touch.
    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable target = other.GetComponentInParent<IInteractable>();
        if (target != null && AllowsTouch(target.Activation))
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

    // Finds an interactable whose Collider2D covers this point. Other colliders
    // under the mouse (the floor, the player) are skipped.
    private IInteractable FindInteractableAt(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint, interactableLayers);
        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                return interactable;
            }
        }
        return null;
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
