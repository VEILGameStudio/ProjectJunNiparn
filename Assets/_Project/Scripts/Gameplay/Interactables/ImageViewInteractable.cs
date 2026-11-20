// ImageViewInteractable
// An object the player clicks to see a picture full screen, for example a calendar on a wall.
// The picture is not here: it lives in a UI panel (a UIPanel prefab inside UIRoot), and this
// script only asks the UIManager to open that panel. The game is paused while it is open
// (tick Pauses Game on the panel). The panel closes with its own close button.
// One panel per picture: to show another picture, make another panel with another Panel Id.
//
// Put this on: the wall object (the calendar). Add a BoxCollider click box first
//   (Is Trigger ticked, layer Interactable) - see InteractableBase for the full setup.
// Assign in Inspector:
//   - Panel Id: the Panel Id of the UIPanel that shows the picture, for example 'CalendarView'.
//     It must be spelled exactly the same. The Console names the id when it is wrong.
//   - Activation / Max Interact Distance / Interact Sound: see InteractableBase.

using UnityEngine;

public class ImageViewInteractable : InteractableBase
{
    [Header("Image Panel")]
    [Tooltip("The Panel Id of the UIPanel that shows the picture full screen, for example 'CalendarView'. Type it exactly as it is written on the panel.")]
    [SerializeField] private string panelId;

    // Says so in the Console when the Panel Id is empty.
    protected override void OnStarted()
    {
        if (string.IsNullOrEmpty(panelId))
        {
            Debug.LogError($"ImageViewInteractable on '{name}': Panel Id is empty. Type the Panel Id of the picture panel (for example 'CalendarView') into Panel Id.", this);
        }
    }

    // Opens the picture panel. The UIManager pauses the game and the panel's button closes it.
    protected override void OnInteract(PlayerContext player)
    {
        if (string.IsNullOrEmpty(panelId))
        {
            return;
        }
        if (UIManager.Instance == null)
        {
            Debug.LogError($"ImageViewInteractable on '{name}': no UIManager found. Make sure the GameSystems prefab (with UIRoot inside it) is in the scene.", this);
            return;
        }
        UIManager.Instance.Open(panelId);
    }
}
