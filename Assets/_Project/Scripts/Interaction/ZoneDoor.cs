// ZoneDoor
// A door or doorway between two camera zones of the SAME scene. When used, the screen
// fades to black, the player is moved to the Destination, and the screen fades back in.
// The CameraZoneSwitcher sees the player is in another zone and switches the camera while
// the screen is black. To lock it, set Required Item and Blocked Message (from InteractableBase).
// For a door between two SCENES use Door instead.
//
// Put this on: the door GameObject.
//   - A door the player clicks: add a BoxCollider click box (Is Trigger ticked, layer
//     Interactable) and set Activation to Click.
//   - A doorway the player walks through: add a child with a trigger on the TouchZone
//     layer and set Activation to Touch.
//   See InteractableBase for the full collider setup.
// Assign in Inspector:
//   - Destination: an empty GameObject placed on the floor of the other zone, just inside
//     its door. Keep it at least one step away from that zone's own doorway trigger.
//   - Activation / Max Interact Distance / Required Item / Blocked Message: see InteractableBase.

using UnityEngine;

public class ZoneDoor : InteractableBase
{
    [Header("Destination")]
    [Tooltip("Where the player arrives: an empty GameObject placed on the floor of the other zone, just inside its door.")]
    [SerializeField] private Transform destination;

    // Moves the player to the Destination behind a fade.
    protected override void OnInteract(PlayerContext player)
    {
        if (destination == null)
        {
            Debug.LogError($"ZoneDoor on '{name}': Destination is not assigned. Drag the empty GameObject where the player should arrive.", this);
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.SceneLoader == null)
        {
            Debug.LogError($"ZoneDoor on '{name}': no SceneLoader was found. Make sure the Managers object (with a SceneLoader) is in the scene.", this);
            return;
        }

        GameManager.Instance.SceneLoader.MovePlayerTo(destination.position);
    }
}
