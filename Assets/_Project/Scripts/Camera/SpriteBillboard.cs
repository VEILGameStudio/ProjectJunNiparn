// SpriteBillboard
// Stands a sprite upright in the 3D world and turns it to face the camera. The camera
// never rotates, so the rotation is set once when the scene starts, not every frame.
// By default the sprite only turns around the vertical axis, so it stays upright and
// meets the floor naturally. Tilt Toward Camera leans it back toward the camera.
// The camera angle is read from the scene's LevelSettings (or from the Main Camera if
// the scene has no LevelSettings).
//
// Put this on: any sprite that should face the camera, for example the Player's
//   "Visual" child, an NPC's sprite, grass, a sign. It rotates the object it is on,
//   so put it on the sprite object, not on a root that carries colliders.
// Assign in Inspector: nothing in the normal case.
//   - Tilt Toward Camera (optional): 0 = stands straight up. A tuning knob for the art
//     team only, not something to change per object.

using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    [Header("Tilt (art team only)")]
    [Tooltip("Leans the sprite back toward the camera, in degrees. 0 = stands straight up (default). It can lean at most as far as the camera looks down. Higher values fight squashing but make the sprite look like it is lying down. Use the same value on every sprite.")]
    [Range(0f, 90f)]
    [SerializeField] private float tiltTowardCamera;

    private void Start()
    {
        Refresh();
    }

    // Sets the rotation from the camera angle. Call it again only if the camera angle changes.
    public void Refresh()
    {
        if (!TryGetCameraAngles(out float cameraPitch, out float cameraYaw))
        {
            Debug.LogWarning($"SpriteBillboard on '{name}': no LevelSettings and no Main Camera found, so the sprite could not be turned to face the camera.", this);
            return;
        }

        transform.rotation = CalculateRotation(cameraPitch, cameraYaw, tiltTowardCamera);
    }

    // The rotation that faces a camera with this pitch and yaw. The tilt is kept between 0 and the pitch.
    // Public and static so tests can check it without a scene.
    public static Quaternion CalculateRotation(float cameraPitch, float cameraYaw, float tiltTowardCamera)
    {
        float tilt = Mathf.Clamp(tiltTowardCamera, 0f, cameraPitch);
        return Quaternion.Euler(tilt, cameraYaw, 0f);
    }

    // Reads the camera angle from LevelSettings, or from the Main Camera if there is no LevelSettings.
    private bool TryGetCameraAngles(out float cameraPitch, out float cameraYaw)
    {
        LevelSettings levelSettings = FindAnyObjectByType<LevelSettings>();
        if (levelSettings != null)
        {
            cameraPitch = levelSettings.CameraPitch;
            cameraYaw = levelSettings.CameraYaw;
            return true;
        }

        if (Camera.main != null)
        {
            Vector3 angles = Camera.main.transform.eulerAngles;
            cameraPitch = angles.x;
            cameraYaw = angles.y;
            return true;
        }

        cameraPitch = 0f;
        cameraYaw = 0f;
        return false;
    }
}
