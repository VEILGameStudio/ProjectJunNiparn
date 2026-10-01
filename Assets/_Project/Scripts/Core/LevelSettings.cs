// LevelSettings
// Sets up the level's camera when the scene starts, so the level designer picks one
// dropdown instead of remembering numbers:
//   - the camera's projection, angle and zoom from the Camera Preset,
//   - how far back the camera sits and how smoothly it follows the player,
//   - the Camera Bounds box, given to the camera's CinemachineConfiner3D.
// The game has two kinds of level, and each level uses one preset:
//   Three Quarter = puzzle room (orthographic, turned 45 degrees),
//   Side View     = corridor (perspective, seen from the side).
// Tick "Override Preset Values" to change the zoom, field of view, distance or smoothing
// for one level. The angle itself can never be overridden and has no field here: every
// sprite is drawn for its preset's angle, so a level with its own angle would have every
// sprite looking wrong.
//
// Put this on: an empty GameObject named "LevelSettings". Every level scene needs
//   exactly one (the player warns in the Console if it is missing).
// Assign in Inspector:
//   - Camera Preset: Three Quarter or Side View. Use the one Oak told you to use.
//   - Level Camera: this level's CinemachineCamera. It needs a CinemachinePositionComposer
//     and a CinemachineConfiner3D, and Rotation Control set to None.
//   - Camera Bounds: a BoxCollider (tick Is Trigger, layer Ignore Raycast) around where
//     the CAMERA may move. The camera sits above and behind the player, so this box
//     sits above and behind the floor, not on it.
// Also needed in a Side View level: select the Main Camera and tick Lens Mode Override
//   on its Cinemachine Brain, or the camera stays orthographic (the Console tells you).

using Unity.Cinemachine;
using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    [Header("Camera Preset")]
    [Tooltip("The kind of level. Three Quarter = puzzle room (orthographic, turned 45 degrees). Side View = corridor (perspective, seen from the side). Use the one Oak told you to use.")]
    [SerializeField] private CameraPreset cameraPreset = CameraPreset.ThreeQuarter;

    [Header("Camera")]
    [Tooltip("This level's CinemachineCamera (the one that follows the player). It needs a CinemachinePositionComposer and a CinemachineConfiner3D.")]
    [SerializeField] private CinemachineCamera levelCamera;

    [Header("Camera Bounds")]
    [Tooltip("A BoxCollider (Is Trigger ticked, layer Ignore Raycast) around where the camera itself may move. The camera sits above and behind the player, so place the box above and behind the floor.")]
    [SerializeField] private Collider cameraBounds;

    [Header("Custom Values")]
    [Tooltip("Tick to use the zoom, field of view, distance and smoothing below instead of the preset's. The camera angle always comes from the preset.")]
    [SerializeField] private bool overridePresetValues;

    [Tooltip("Three Quarter levels only. Zoom: half the screen height in world units. Bigger = sees more. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float orthographicSize = 5f;

    [Tooltip("Side View levels only. How wide the camera sees, in degrees. Bigger = sees more, but things shrink faster with distance. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float fieldOfView = 25f;

    [Tooltip("How far back from the player the camera sits. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float cameraDistance = 20f;

    [Tooltip("Camera follow smoothing: X = left/right on screen, Y = up/down on screen (higher = softer). Only used when Override Preset Values is ticked.")]
    [SerializeField] private Vector2 cameraDamping = new Vector2(1f, 1.3f);

    // How many degrees the camera looks down. SpriteBillboard reads this.
    public float CameraPitch => GetAppliedValues().Pitch;

    // Which way the camera faces around the vertical axis. SpriteBillboard reads this.
    public float CameraYaw => GetAppliedValues().Yaw;

    private void Start()
    {
        WarnIfMoreThanOne();
        ApplyToLevelCamera();
    }

    // The numbers this level really uses: the preset's, with the custom values put in when overriding.
    // This is the only place that looks up the preset, so the camera and the sprites always agree.
    public CameraPresetValues GetAppliedValues()
    {
        CameraPresetValues values = CameraPresetValues.For(cameraPreset);
        if (overridePresetValues)
        {
            values.OrthographicSize = orthographicSize;
            values.FieldOfView = fieldOfView;
            values.CameraDistance = cameraDistance;
            values.Damping = cameraDamping;
        }
        return values;
    }

    // Sets up the Level Camera: preset, bounds, and a check that the projection will really change.
    public void ApplyToLevelCamera()
    {
        if (levelCamera == null)
        {
            Debug.LogError($"LevelSettings on '{name}': Level Camera is not assigned, so the camera angle, zoom and bounds were not set. Drag this level's CinemachineCamera here.", this);
            return;
        }

        CameraPresetValues values = GetAppliedValues();
        CameraPresetValues.Apply(levelCamera, values);
        ApplyBounds();
        CheckProjection(values);
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

    // Says so in the Console when the Main Camera would not get the preset's projection.
    // The Cinemachine Brain is only read here; ticking Lens Mode Override is done in the Inspector.
    private void CheckProjection(CameraPresetValues values)
    {
        CinemachineBrain brain = FindAnyObjectByType<CinemachineBrain>();
        string problem = CameraPresetValues.FindProjectionProblem(levelCamera, brain, values);
        if (problem != null)
        {
            Debug.LogError($"LevelSettings on '{name}': {problem}", this);
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
