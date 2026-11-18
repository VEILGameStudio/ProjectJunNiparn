// CameraFlipButton
// The on-screen button that flips the camera, the same as pressing Q. It only shows in an area
// that allows a flip (Allow Camera Flip ticked on its LevelSettings) and is hidden everywhere else.
// It finds the scene's CameraFlipController by itself, so it can live in a UI prefab that
// survives scene loads.
// Put this on: the UI Button GameObject (a Button component is required).
// Assign in Inspector: nothing.

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class CameraFlipButton : MonoBehaviour
{
    private Button button;
    private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        button.onClick.AddListener(HandleClicked);
        GameEvents.OnCameraZoneChanged += HandleCameraZoneChanged;
        GameEvents.OnSceneReady += RefreshVisibility;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleClicked);
        GameEvents.OnCameraZoneChanged -= HandleCameraZoneChanged;
        GameEvents.OnSceneReady -= RefreshVisibility;
    }

    // Asks the scene's controller to flip.
    private void HandleClicked()
    {
        CameraFlipController controller = FindAnyObjectByType<CameraFlipController>();
        if (controller == null)
        {
            Debug.LogError($"CameraFlipButton on '{name}': no CameraFlipController found in this scene. Add one to the Managers object.", this);
            return;
        }

        controller.TryFlip();
    }

    private void HandleCameraZoneChanged(CameraPreset preset)
    {
        RefreshVisibility();
    }

    // Shows the button only where the area allows a flip. A CanvasGroup hides it without
    // switching the object off, so this script keeps hearing events.
    private void RefreshVisibility()
    {
        CameraFlipController controller = FindAnyObjectByType<CameraFlipController>();
        bool visible = controller != null && controller.AreaAllowsFlip();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
