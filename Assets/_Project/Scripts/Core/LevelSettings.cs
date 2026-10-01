// LevelSettings
// Sets up the level's camera when the scene starts, so the level designer picks one
// dropdown instead of remembering numbers:
//   - the camera angle and zoom from the Camera Preset,
//   - how far back the camera sits and how smoothly it follows the player,
//   - the Camera Bounds box, given to the camera's CinemachineConfiner3D.
// The camera angle is the same in every level, because every sprite is drawn for it.
// Tick "Override Preset Values" to change the zoom, distance or smoothing for one level.
// The angle itself can never be overridden.
//
// Put this on: an empty GameObject named "LevelSettings". Every level scene needs
//   exactly one (the player warns in the Console if it is missing).
// Assign in Inspector:
//   - Camera Preset: leave on Default.
//   - Level Camera: this level's CinemachineCamera. It needs a CinemachinePositionComposer
//     and a CinemachineConfiner3D, and Rotation Control set to None.
//   - Camera Bounds: a BoxCollider (tick Is Trigger, layer Ignore Raycast) around where
//     the CAMERA may move. The camera sits above and behind the player, so this box
//     sits above and behind the floor, not on it.

using Unity.Cinemachine;
using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    [Header("Camera Preset")]
    [Tooltip("The camera angle, zoom and smoothing for this level. The whole game uses Default.")]
    [SerializeField] private CameraPreset cameraPreset = CameraPreset.Default;

    [Header("Camera")]
    [Tooltip("This level's CinemachineCamera (the one that follows the player). It needs a CinemachinePositionComposer and a CinemachineConfiner3D.")]
    [SerializeField] private CinemachineCamera levelCamera;

    [Header("Camera Bounds")]
    [Tooltip("A BoxCollider (Is Trigger ticked, layer Ignore Raycast) around where the camera itself may move. The camera sits above and behind the player, so place the box above and behind the floor.")]
    [SerializeField] private Collider cameraBounds;

    [Header("Custom Values")]
    [Tooltip("Tick to use the zoom, distance and smoothing below instead of the preset's. The camera angle always comes from the preset.")]
    [SerializeField] private bool overridePresetValues;

    [Tooltip("Zoom: half the screen height in world units. Bigger = sees more. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float orthographicSize = 5f;

    [Tooltip("How far back from the player the camera sits. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float cameraDistance = 20f;

    [Tooltip("Camera follow smoothing: X = left/right on screen, Y = up/down on screen (higher = softer). Only used when Override Preset Values is ticked.")]
    [SerializeField] private Vector2 cameraDamping = new Vector2(1f, 1.3f);

    // How many degrees the camera looks down. SpriteBillboard reads this.
    public float CameraPitch => CameraPresetValues.For(cameraPreset).Pitch;

    // Which way the camera faces around the vertical axis. SpriteBillboard reads this.
    public float CameraYaw => CameraPresetValues.For(cameraPreset).Yaw;

    private void Start()
    {
        WarnIfMoreThanOne();
        WarnIfMainCameraNotOrthographic();

        if (levelCamera == null)
        {
            Debug.LogError($"LevelSettings on '{name}': Level Camera is not assigned, so the camera angle, zoom and bounds were not set. Drag this level's CinemachineCamera here.", this);
            return;
        }

        CameraPresetValues values = GetValues();
        ApplyAngle(values);
        ApplyZoom(values);
        ApplyFollow(values);
        ApplyBounds();
    }

    // Returns the preset's numbers, with zoom, distance and smoothing replaced when overriding.
    private CameraPresetValues GetValues()
    {
        CameraPresetValues values = CameraPresetValues.For(cameraPreset);
        if (overridePresetValues)
        {
            values.OrthographicSize = orthographicSize;
            values.CameraDistance = cameraDistance;
            values.Damping = cameraDamping;
        }
        return values;
    }

    // Turns the CinemachineCamera to the game's camera angle. Cinemachine then moves the Main Camera.
    private void ApplyAngle(CameraPresetValues values)
    {
        if (levelCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim) != null)
        {
            Debug.LogError($"LevelSettings on '{name}': the Level Camera has a Rotation Control, which turns the camera away from the game's camera angle. Select the CinemachineCamera and set Rotation Control to None.", this);
        }

        Quaternion angle = Quaternion.Euler(values.Pitch, values.Yaw, 0f);
        if (Quaternion.Angle(levelCamera.transform.rotation, angle) > 0.1f)
        {
            Debug.LogWarning($"LevelSettings on '{name}': the Level Camera was turned to the game's camera angle ({values.Pitch}, {values.Yaw}, 0). Type that into its Rotation in the Inspector, so the Scene view looks like the game.", this);
        }
        levelCamera.transform.rotation = angle;
    }

    // Sets how much of the level the camera shows.
    private void ApplyZoom(CameraPresetValues values)
    {
        LensSettings lens = levelCamera.Lens;
        lens.OrthographicSize = values.OrthographicSize;
        levelCamera.Lens = lens;
    }

    // Sets how far back the camera sits and how smoothly it follows the player.
    private void ApplyFollow(CameraPresetValues values)
    {
        CinemachinePositionComposer composer = levelCamera.GetComponent<CinemachinePositionComposer>();
        if (composer == null)
        {
            Debug.LogError($"LevelSettings on '{name}': the Level Camera has no CinemachinePositionComposer, so it cannot follow the player. Select the CinemachineCamera and set Position Control to Position Composer.", this);
            return;
        }

        composer.CameraDistance = values.CameraDistance;
        composer.Damping = new Vector3(values.Damping.x, values.Damping.y, composer.Damping.z);
    }

    // Gives the Camera Bounds box to the camera's confiner.
    private void ApplyBounds()
    {
        CinemachineConfiner3D confiner = levelCamera.GetComponent<CinemachineConfiner3D>();
        if (confiner == null)
        {
            Debug.LogError($"LevelSettings on '{name}': the Level Camera has no CinemachineConfiner3D, so the camera has no bounds. Select the CinemachineCamera and add Cinemachine Confiner 3D.", this);
            return;
        }
        if (cameraBounds == null)
        {
            Debug.LogWarning($"LevelSettings on '{name}': Camera Bounds is not assigned, so the camera can move anywhere. Drag this level's camera bounds BoxCollider here.", this);
            return;
        }
        if (!cameraBounds.isTrigger)
        {
            Debug.LogWarning($"LevelSettings on '{name}': Camera Bounds should have Is Trigger ticked, or things can bump into the box.", this);
        }

        confiner.BoundingVolume = cameraBounds;
    }

    // The game needs an orthographic camera: no perspective, things look the same size near and far.
    private void WarnIfMainCameraNotOrthographic()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && !mainCamera.orthographic)
        {
            Debug.LogError($"LevelSettings on '{name}': the Main Camera is not Orthographic. Select the Main Camera and set Projection to Orthographic.", this);
        }
    }

    // Each scene must have exactly one LevelSettings.
    private void WarnIfMoreThanOne()
    {
        LevelSettings[] all = FindObjectsByType<LevelSettings>();
        if (all.Length > 1)
        {
            Debug.LogError($"LevelSettings: this scene has {all.Length} LevelSettings. Keep only one and delete the others.", this);
        }
    }
}
