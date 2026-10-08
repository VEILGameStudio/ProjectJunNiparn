// LevelSettings
// Sets up one area's camera, so the level designer picks one dropdown and draws one box
// instead of remembering numbers:
//   - the camera's projection, angle, zoom, distance and smoothing come from the Camera Preset,
//   - the camera's limits are worked out from the Play Area: the box the PLAYER can walk in.
//     The camera stops when the edge of the screen reaches the edge of that box. It is worked
//     out again when the window changes shape, so it is right on every monitor.
// The game has two kinds of area, and each area uses one preset:
//   Three Quarter = puzzle room (orthographic, turned 45 degrees),
//   Side View     = corridor (perspective, seen from the side).
// Tick "Override Preset Values" to change the zoom, field of view, distance or smoothing
// for one area. The angle itself can never be overridden and has no field here: every
// sprite is drawn for its preset's angle, so an area with its own angle would have every
// sprite looking wrong.
// Never change the camera's own numbers in the scene by hand. This script writes them
// when the level starts.
//
// Put this on: an empty GameObject named "LevelSettings".
//   - A normal level needs exactly one.
//   - A level with Camera Zones needs one per zone, inside that zone's Content object.
//     Only one zone is switched on at a time, so only one LevelSettings is ever active.
// Assign in Inspector:
//   - Camera Preset: Three Quarter or Side View. Use the one Oak told you to use.
//   - Level Camera: this area's CinemachineCamera. It needs a CinemachinePositionComposer
//     and a CinemachineConfiner3D, and Rotation Control set to None.
//   - Play Area: a BoxCollider (tick Is Trigger, layer Ignore Raycast) around where the
//     PLAYER can walk: from wall to wall, and from the floor up to the highest place the
//     player can stand. In a camera zone, drag the zone's own box here.
// Also needed in a Side View level: select the Main Camera and tick Lens Mode Override
//   on its Cinemachine Brain, or the camera stays orthographic (the Console tells you).

using Unity.Cinemachine;
using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    [Header("Camera Preset")]
    [Tooltip("The kind of area. Three Quarter = puzzle room (orthographic, turned 45 degrees). Side View = corridor (perspective, seen from the side). Use the one Oak told you to use.")]
    [SerializeField] private CameraPreset cameraPreset = CameraPreset.ThreeQuarter;

    [Header("Camera")]
    [Tooltip("This area's CinemachineCamera (the one that follows the player). It needs a CinemachinePositionComposer and a CinemachineConfiner3D.")]
    [SerializeField] private CinemachineCamera levelCamera;

    [Header("Play Area")]
    [Tooltip("A BoxCollider (Is Trigger ticked, layer Ignore Raycast) around where the PLAYER can walk: from wall to wall, and from the floor up to the highest place the player can stand. The camera's limits are worked out from this box. In a camera zone, drag the zone's own box here.")]
    [SerializeField] private BoxCollider playArea;

    [Tooltip("Deprecated. An old box placed by hand around where the CAMERA may sit. It is right for one screen shape only, and it is used only while Play Area is empty. Assign Play Area instead, then clear this field.")]
    [SerializeField] private Collider cameraBounds;

    [Header("Custom Values")]
    [Tooltip("Tick to use the zoom, field of view, distance and smoothing below instead of the preset's. The camera angle always comes from the preset.")]
    [SerializeField] private bool overridePresetValues;

    [Tooltip("Three Quarter areas only. Zoom: half the screen height in world units. Bigger = sees more. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float orthographicSize = 5f;

    [Tooltip("Side View areas only. How wide the camera sees, in degrees. Bigger = sees more, but things shrink faster with distance. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float fieldOfView = 25f;

    [Tooltip("How far back from the player the camera sits. Only used when Override Preset Values is ticked.")]
    [SerializeField] private float cameraDistance = 20f;

    [Tooltip("Camera follow smoothing: X = left/right on screen, Y = up/down on screen, Z = toward/away from the camera (higher = softer). Only used when Override Preset Values is ticked.")]
    [SerializeField] private Vector3 followDamping = new Vector3(1f, 1f, 1.3f);

    // The box the camera may move in. It is made while the game runs, from the Play Area.
    private BoxCollider cameraBox;

    // The camera that shows the picture, and the screen shape the camera box was made for.
    private Camera screenCamera;
    private float appliedAspect;

    // Which preset this area uses. PlayerAnimator reads this to pick the right drawings.
    public CameraPreset Preset => cameraPreset;

    // How many degrees the camera looks down.
    public float CameraPitch => GetAppliedValues().Pitch;

    // Which way the camera faces around the vertical axis. SpriteBillboard reads this.
    public float CameraYaw => GetAppliedValues().Yaw;

    private void Start()
    {
        WarnIfMoreThanOne();
        ApplyToLevelCamera();
    }

    // Keeps the camera box switched on only while this area is (a camera zone switches its area off).
    private void OnEnable()
    {
        if (cameraBox != null)
        {
            cameraBox.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (cameraBox != null)
        {
            cameraBox.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (cameraBox != null)
        {
            Destroy(cameraBox.gameObject);
        }
    }

    // Works the camera's limits out again when the window changes shape (a wider screen sees more).
    private void Update()
    {
        if (cameraBox != null && !Mathf.Approximately(GetScreenAspect(), appliedAspect))
        {
            ApplyBounds();
        }
    }

    // The numbers this area really uses: the preset's, with the custom values put in when overriding.
    // This is the only place that looks up the preset, so the camera and the sprites always agree.
    public CameraPresetValues GetAppliedValues()
    {
        CameraPresetValues values = CameraPresetValues.For(cameraPreset);
        if (overridePresetValues)
        {
            values.OrthographicSize = orthographicSize;
            values.FieldOfView = fieldOfView;
            values.CameraDistance = cameraDistance;
            values.Damping = followDamping;
        }
        return values;
    }

    // Sets up the Level Camera: preset, limits, and a check that the projection will really change.
    public void ApplyToLevelCamera()
    {
        if (levelCamera == null)
        {
            Debug.LogError($"LevelSettings on '{name}': Level Camera is not assigned, so the camera angle, zoom and limits were not set. Drag this area's CinemachineCamera here.", this);
            return;
        }

        CameraPresetValues values = GetAppliedValues();
        CameraPresetValues.Apply(levelCamera, values);
        ApplyBounds();
        CheckProjection(values);
    }

    // Gives the camera its limits: worked out from the Play Area, or the old hand-placed box if that is all there is.
    private void ApplyBounds()
    {
        CinemachineConfiner3D confiner = levelCamera.GetComponent<CinemachineConfiner3D>();
        if (confiner == null)
        {
            Debug.LogError($"LevelSettings on '{name}': the Level Camera has no CinemachineConfiner3D, so the camera has no limits. Select the CinemachineCamera and add Cinemachine Confiner 3D.", this);
            return;
        }

        if (playArea != null)
        {
            ApplyPlayArea(confiner);
            return;
        }

        if (cameraBounds != null)
        {
            Debug.LogWarning($"LevelSettings on '{name}': this area still uses the old hand-placed Camera Bounds box, which is right for one screen shape only. Add a BoxCollider (Is Trigger ticked, layer Ignore Raycast) around where the player can walk, drag it into Play Area, then clear Camera Bounds.", this);
            confiner.BoundingVolume = cameraBounds;
            return;
        }

        Debug.LogWarning($"LevelSettings on '{name}': Play Area is not assigned, so the camera can move anywhere. Add a BoxCollider (Is Trigger ticked, layer Ignore Raycast) around where the player can walk and drag it here.", this);
    }

    // Works out the camera box from the Play Area and the screen shape, and gives it to the confiner.
    private void ApplyPlayArea(CinemachineConfiner3D confiner)
    {
        if (!Application.isPlaying)
        {
            return; // The camera box only exists while the game runs.
        }

        if (cameraBox == null)
        {
            cameraBox = CreateCameraBox();
        }

        Transform area = playArea.transform;
        Vector3 areaCenter = area.TransformPoint(playArea.center);
        Vector3 areaScale = area.lossyScale;
        Vector3 areaSize = new Vector3(
            Mathf.Abs(playArea.size.x * areaScale.x),
            Mathf.Abs(playArea.size.y * areaScale.y),
            Mathf.Abs(playArea.size.z * areaScale.z));

        CameraPresetValues values = GetAppliedValues();
        float aspect = GetScreenAspect();
        CameraPresetValues.CalculateCameraBox(areaCenter, areaSize, area.rotation, values, aspect, out Vector3 boxCenter, out Vector3 boxSize);

        cameraBox.transform.SetPositionAndRotation(boxCenter, Quaternion.Euler(0f, values.Yaw, 0f));
        cameraBox.size = boxSize;
        Physics.SyncTransforms(); // The confiner asks the physics engine where the box is, so tell it the box moved.

        confiner.BoundingVolume = cameraBox;
        appliedAspect = aspect;
    }

    // Makes the empty box object the camera is kept inside.
    private BoxCollider CreateCameraBox()
    {
        GameObject boxObject = new GameObject($"Camera Box (made by {name})");
        boxObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        BoxCollider box = boxObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        return box;
    }

    // Screen width divided by screen height, read from the camera that shows the picture.
    private float GetScreenAspect()
    {
        if (screenCamera == null)
        {
            CinemachineBrain brain = FindAnyObjectByType<CinemachineBrain>();
            screenCamera = brain != null ? brain.OutputCamera : Camera.main;
        }

        if (screenCamera == null)
        {
            return 16f / 9f;
        }
        return screenCamera.aspect;
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

    // Only one LevelSettings may be switched on at a time.
    private void WarnIfMoreThanOne()
    {
        LevelSettings[] all = FindObjectsByType<LevelSettings>();
        if (all.Length > 1)
        {
            Debug.LogError($"LevelSettings: {all.Length} LevelSettings are switched on at once. A normal level keeps only one: delete the others. In a level with Camera Zones, each LevelSettings must be inside its zone's Content object, so that only the zone the player stands in has one switched on.", this);
        }
    }
}
