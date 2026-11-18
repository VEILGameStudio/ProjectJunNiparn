// FlipWallSwitcher
// Shows one group of objects while the Side View camera is on its normal side, and another group
// while the camera is flipped (Q key or the flip button). Use it for walls that would block the
// camera after a flip, and for the invisible blocking colliders that keep the player in the corridor.
// Example: the wall in front of the camera is switched OFF when flipped, and the matching wall on
// the other side is switched ON.
// It only listens: it never flips the camera and never raises an event.
//
// Put this on: an always-on object of the area (for example the area's Content or a "Walls" parent).
//   Do NOT put it on an object that is in one of the two lists, or it switches itself off.
// Assign in Inspector:
//   - Normal Side Objects: objects that are ON when the camera is on its normal side.
//   - Flipped Side Objects: objects that are ON when the camera is flipped.

using System.Collections;
using UnityEngine;

public class FlipWallSwitcher : MonoBehaviour
{
    [Header("Objects")]
    [Tooltip("Objects that are ON while the camera is on its normal side, and OFF while it is flipped. Walls, colliders or props.")]
    [SerializeField] private GameObject[] normalSideObjects;

    [Tooltip("Objects that are ON while the camera is flipped, and OFF while it is on its normal side.")]
    [SerializeField] private GameObject[] flippedSideObjects;

    // True when the normal side group should be on. Public and static so tests can check it without a scene.
    public static bool ShouldShowNormalSide(bool cameraIsFlipped)
    {
        return !cameraIsFlipped;
    }

    private void OnEnable()
    {
        GameEvents.OnCameraZoneChanged += HandleCameraZoneChanged;
        GameEvents.OnSceneReady += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnCameraZoneChanged -= HandleCameraZoneChanged;
        GameEvents.OnSceneReady -= Refresh;
    }

    // Refreshes now, and once more next frame. When a zone opens, the order in which listeners
    // hear this event is not fixed, so the camera side may only be restored a moment later.
    private void HandleCameraZoneChanged(CameraPreset preset)
    {
        Refresh();
        if (isActiveAndEnabled)
        {
            StartCoroutine(RefreshNextFrame());
        }
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        Refresh();
    }

    // Reads the camera side from the area's LevelSettings and switches the two groups.
    // Safe to run many times: it sets the state, it never toggles.
    private void Refresh()
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        if (settings == null)
        {
            Debug.LogWarning($"FlipWallSwitcher on '{name}': no active LevelSettings found, so the normal side is shown. Put this script inside the area's Content.", this);
        }

        bool cameraIsFlipped = settings != null && settings.IsFlipped;
        ApplyGroups(normalSideObjects, flippedSideObjects, cameraIsFlipped, this);
    }

    // Switches the two groups for the given camera side. Public and static so tests can run it without a scene.
    public static void ApplyGroups(GameObject[] normalGroup, GameObject[] flippedGroup, bool cameraIsFlipped, FlipWallSwitcher owner)
    {
        bool showNormal = ShouldShowNormalSide(cameraIsFlipped);
        SetGroupActive(normalGroup, showNormal, owner);
        SetGroupActive(flippedGroup, !showNormal, owner);
    }

    // Switches every object in a list on or off, and warns about empty slots.
    private static void SetGroupActive(GameObject[] group, bool active, FlipWallSwitcher owner)
    {
        if (group == null)
        {
            return;
        }

        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == null)
            {
                string ownerName = owner != null ? owner.name : "unknown";
                Debug.LogWarning($"FlipWallSwitcher on '{ownerName}': a slot in the lists is empty. Drag a wall or collider into it, or remove the slot.", owner);
                continue;
            }

            if (owner != null && group[i] == owner.gameObject)
            {
                Debug.LogWarning($"FlipWallSwitcher on '{owner.name}': this object is in its own list, so it would switch itself off. Move the script to a parent object.", owner);
                continue;
            }

            if (group[i].activeSelf != active)
            {
                group[i].SetActive(active);
            }
        }
    }
}
