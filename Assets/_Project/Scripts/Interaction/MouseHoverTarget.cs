// MouseHoverTarget
// Finds which clickable object the mouse is pointing at. The highlight scripts
// (HighlightEffect, MeshHighlightEffect) ask it every frame.
// The raycast runs only once per frame, however many highlights ask.
// It finds nothing while the mouse is over UI, while the game is not playing,
// or when there is no mouse (gamepad only).
//
// Put this on: nothing. It is a static helper, not a component.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class MouseHoverTarget
{
    private static int lastFrame = -1;
    private static Transform hoveredRoot;

    // Clears the saved result when Play mode starts (safe if Domain Reload is off).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        lastFrame = -1;
        hoveredRoot = null;
    }

    // True when the mouse points at the clickable object that this transform belongs to.
    public static bool IsHovering(Transform effectTransform)
    {
        UpdateHoveredRoot();
        return hoveredRoot != null && effectTransform.IsChildOf(hoveredRoot);
    }

    // Does the one raycast for this frame and remembers the result.
    private static void UpdateHoveredRoot()
    {
        if (lastFrame == Time.frameCount)
        {
            return;
        }
        lastFrame = Time.frameCount;
        hoveredRoot = FindHoveredRoot();
    }

    // Raycasts from the main camera through the mouse onto the Interactable layer.
    private static Transform FindHoveredRoot()
    {
        if (Mouse.current == null)
        {
            return null;
        }
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return null;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return null;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return null;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GameLayers.InteractableMask, QueryTriggerInteraction.Collide))
        {
            return null;
        }

        IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
        Component targetComponent = target as Component;
        if (targetComponent == null)
        {
            return null;
        }
        if (target.Activation == ActivationMode.Touch)
        {
            return null; // Touch-only objects cannot be clicked, so no highlight.
        }
        return targetComponent.transform;
    }
}
