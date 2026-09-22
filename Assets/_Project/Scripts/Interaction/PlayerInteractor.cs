// PlayerInteractor
// Turns a mouse click into an interaction. When the player clicks, it shoots a ray
// from the camera through the mouse pointer to find what was clicked, then calls
// Interact on it. It works in both game modes:
//   - 2D scenes use a 2D physics ray (needs Collider2D on interactables).
//   - 2.5D scenes use a 3D physics ray (needs a 3D Collider on interactables).
// Choose which one with the Raycast Mode field.
//
// Put this on: the Player GameObject.
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Interaction Camera (optional): leave empty to use the Main Camera.
//   - Raycast Mode: 2D for side-view scenes, 2.5D for top-down scenes.
//   - Interactable Layers: which layers can be clicked (leave as Everything to start).

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    // Which kind of physics ray to use, chosen per scene type.
    private enum RaycastMode { Mode2D, Mode25D }

    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Camera")]
    [Tooltip("The camera clicks are measured from. Leave empty to use the Main Camera.")]
    [SerializeField] private Camera interactionCamera;

    [Header("Raycast")]
    [Tooltip("Use 2D for side-view scenes and 2.5D for top-down 3D scenes.")]
    [SerializeField] private RaycastMode raycastMode = RaycastMode.Mode2D;

    [Tooltip("Which layers can be clicked on. Leave as Everything to start.")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Tooltip("How far the 2.5D ray reaches, in units.")]
    [SerializeField] private float maxDistance = 100f;

    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
        if (inputReader == null)
        {
            Debug.LogError($"PlayerInteractor on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.OnInteract += HandleInteract;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnInteract -= HandleInteract;
        }
    }

    // Runs when the player clicks. Finds what was clicked and interacts with it.
    private void HandleInteract()
    {
        if (!CanInteract() || interactionCamera == null || Mouse.current == null)
        {
            return;
        }

        // Ignore clicks that land on UI (like the inventory window).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        IInteractable interactable = raycastMode == RaycastMode.Mode25D
            ? Raycast3D(screenPosition)
            : Raycast2D(screenPosition);

        if (interactable != null)
        {
            interactable.Interact(gameObject);

            // Reset the idle hint timer whenever the player interacts.
            if (HintManager.Instance != null)
            {
                HintManager.Instance.NotifyInteraction();
            }
        }
    }

    // The player can only interact during normal gameplay.
    private bool CanInteract()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // Finds an interactable under the pointer using 3D physics (for 2.5D scenes).
    private IInteractable Raycast3D(Vector2 screenPosition)
    {
        Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers))
        {
            return hit.collider.GetComponentInParent<IInteractable>();
        }
        return null;
    }

    // Finds an interactable under the pointer using 2D physics (for 2D scenes).
    private IInteractable Raycast2D(Vector2 screenPosition)
    {
        Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactableLayers);
        if (hit.collider != null)
        {
            return hit.collider.GetComponentInParent<IInteractable>();
        }
        return null;
    }
}
