// SpriteBillboard
// Stands a sprite upright in the 3D world and turns it to face the camera. The camera
// never rotates, so the rotation is set once when the sprite appears, not every frame.
// The sprite only turns around the vertical axis: the card always stands straight up, so
// it meets the floor naturally. There is no tilt and there must never be one: the drawings
// are already stretched taller to make up for the camera looking down, so tilting the card
// as well would make every character too tall.
// The camera angle is read from the area's LevelSettings (or from the Main Camera if the
// scene has no LevelSettings). When the player walks into another camera zone, the
// CameraZoneSwitcher calls RefreshAll() and every sprite that is switched on turns again.
//
// Put this on: any sprite that should face the camera, for example the Player's
//   "Visual" child, an NPC's sprite, grass, a sign. It rotates the object it is on,
//   so put it on the sprite object, not on a root that carries colliders.
// Assign in Inspector: nothing.

using System.Collections.Generic;
using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    // Every billboard that is switched on right now. RefreshAll goes through this list.
    private static readonly List<SpriteBillboard> activeBillboards = new List<SpriteBillboard>();

    private void OnEnable()
    {
        activeBillboards.Add(this);
    }

    private void OnDisable()
    {
        activeBillboards.Remove(this);
    }

    private void Start()
    {
        Refresh();
    }

    // Turns this sprite to face the camera. Call it again only if the camera angle changes.
    public void Refresh()
    {
        if (!TryGetCameraYaw(out float cameraYaw))
        {
            Debug.LogWarning($"SpriteBillboard on '{name}': no LevelSettings and no Main Camera found, so the sprite could not be turned to face the camera.", this);
            return;
        }

        transform.rotation = CalculateRotation(cameraYaw);
    }

    // Turns every switched-on sprite to face the camera again. The CameraZoneSwitcher calls
    // this after it switched to another camera zone.
    public static void RefreshAll()
    {
        if (!TryGetCameraYaw(out float cameraYaw))
        {
            return;
        }

        Quaternion rotation = CalculateRotation(cameraYaw);
        foreach (SpriteBillboard billboard in activeBillboards)
        {
            billboard.transform.rotation = rotation;
        }
    }

    // The rotation that faces a camera turned by this yaw. The card stays upright.
    // Public and static so tests can check it without a scene.
    public static Quaternion CalculateRotation(float cameraYaw)
    {
        return Quaternion.Euler(0f, cameraYaw, 0f);
    }

    // Reads which way the camera is turned: from the LevelSettings that is switched on,
    // or from the Main Camera if there is no LevelSettings. BlobShadow uses this too.
    public static bool TryGetCameraYaw(out float cameraYaw)
    {
        LevelSettings levelSettings = FindAnyObjectByType<LevelSettings>();
        if (levelSettings != null)
        {
            cameraYaw = levelSettings.CameraYaw;
            return true;
        }

        if (Camera.main != null)
        {
            cameraYaw = Camera.main.transform.eulerAngles.y;
            return true;
        }

        cameraYaw = 0f;
        return false;
    }
}
