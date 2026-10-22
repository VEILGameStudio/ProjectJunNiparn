// PlayerAnimator
// Picks the player's sprite animation: which way the character faces, and whether it is
// standing or walking. It uses a Unity Animator, with one Animator Controller per camera
// preset, because the two presets use different drawings:
//   Three Quarter - 8 facings (up, up-right, right, down-right, down, ...), each drawn separately.
//   Side View     - drawn facing right only. Facing left is the same drawing mirrored (Scale X = -1).
// The facing comes from the direction the player walks ON THE SCREEN, not in the world.
// So pressing W always shows the character's back, whichever way the camera is turned.
// When the player walks into another camera zone, the controller is swapped to the new preset's.
//
// Put this on: the Player's "Visual" child (the one with the SpriteRenderer). An Animator
//   is added automatically. PlayerMovement on the Player root stops mirroring the Visual
//   by itself once this script is here.
// Assign in Inspector:
//   - Three Quarter Controller: the Animator Controller made from the Three Quarter drawings.
//   - Side View Controller: the Animator Controller made from the Side View drawings.
// Both controllers need these parameters: FaceX (Float), FaceY (Float), IsMoving (Bool).
//   FaceX / FaceY is the facing on screen: (0, 1) = up, (1, 0) = right, (0, -1) = down.
// To add a new animation later (interact, pick up), add a state and a Trigger parameter
//   to both controllers in the Animator window; no change is needed here to keep walking working.

using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Animator Controllers")]
    [Tooltip("The Animator Controller for Three Quarter areas (8 facings). Its drawings come from Art/Sprites/ThreeQuarter.")]
    [SerializeField] private RuntimeAnimatorController threeQuarterController;

    [Tooltip("The Animator Controller for Side View areas (drawn facing right, mirrored for left). Its drawings come from Art/Sprites/SideView.")]
    [SerializeField] private RuntimeAnimatorController sideViewController;

    private static readonly int FaceXId = Animator.StringToHash("FaceX");
    private static readonly int FaceYId = Animator.StringToHash("FaceY");
    private static readonly int IsMovingId = Animator.StringToHash("IsMoving");

    private Animator animator;
    private PlayerMovement movement;
    private CameraPreset currentPreset;

    // Which way the character faces on screen. It starts facing the camera (down the screen).
    private Vector2 facing = Vector2.down;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponentInParent<PlayerMovement>();

        if (movement == null)
        {
            Debug.LogError($"PlayerAnimator on '{name}': no PlayerMovement found on the parent, so it cannot tell which way the player walks. Put this script on the Visual child of the Player.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCameraZoneChanged += UsePreset;
    }

    private void OnDisable()
    {
        GameEvents.OnCameraZoneChanged -= UsePreset;
    }

    private void Start()
    {
        LevelSettings levelSettings = FindAnyObjectByType<LevelSettings>();
        if (levelSettings == null)
        {
            Debug.LogWarning($"PlayerAnimator on '{name}': this scene has no LevelSettings, so the Three Quarter drawings are used. Add a LevelSettings and pick its Camera Preset.", this);
            UsePreset(CameraPreset.ThreeQuarter);
            return;
        }

        UsePreset(levelSettings.Preset);
    }

    // Tells the Animator which way the character faces and whether it is walking.
    private void Update()
    {
        if (movement == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        Vector2 screenDirection = movement.ScreenMoveDirection;
        bool isMoving = screenDirection.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            if (currentPreset == CameraPreset.SideView)
            {
                facing = SnapToLeftRight(screenDirection, facing);
            }
            else
            {
                facing = SnapToEightDirections(screenDirection);
            }
        }

        animator.SetFloat(FaceXId, facing.x);
        animator.SetFloat(FaceYId, facing.y);
        animator.SetBool(IsMovingId, isMoving);
        MirrorForSideView();
    }

    // Swaps to the drawings of a camera preset. Runs at the start and whenever the camera zone changes.
    private void UsePreset(CameraPreset preset)
    {
        currentPreset = preset;

        RuntimeAnimatorController controller = threeQuarterController;
        if (preset == CameraPreset.SideView)
        {
            controller = sideViewController;
            facing = SnapToLeftRight(facing, Vector2.right);
        }

        if (controller == null)
        {
            Debug.LogError($"PlayerAnimator on '{name}': no Animator Controller is assigned for the {preset} preset, so the player has no animation there. Drag the controller into the matching field.", this);
            return;
        }

        animator.runtimeAnimatorController = controller;
        MirrorForSideView();
    }

    // Side View drawings only face right, so facing left mirrors the Visual. Three Quarter drawings are never mirrored.
    private void MirrorForSideView()
    {
        float side = 1f;
        if (currentPreset == CameraPreset.SideView && facing.x < 0f)
        {
            side = -1f;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * side;
        transform.localScale = scale;
    }

    // Rounds a direction on the screen to the nearest of the 8 facings the sprites are drawn for.
    // The direction must already be a SCREEN direction (X = right, Y = up), never a world direction.
    // Public and static so tests can check it without a scene.
    public static Vector2 SnapToEightDirections(Vector2 screenDirection)
    {
        float angle = Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;

        // Rounded, so "up" is exactly (0, 1) and not (0.0000001, 1).
        float x = Mathf.Round(Mathf.Cos(snappedAngle) * 1000f) / 1000f;
        float y = Mathf.Round(Mathf.Sin(snappedAngle) * 1000f) / 1000f;
        return new Vector2(x, y);
    }

    // Rounds a direction on the screen to left or right. Walking straight up or down the
    // screen keeps the side the character was already facing.
    // Public and static so tests can check it without a scene.
    public static Vector2 SnapToLeftRight(Vector2 screenDirection, Vector2 currentFacing)
    {
        float horizontal = screenDirection.x;
        if (Mathf.Abs(horizontal) < 0.01f)
        {
            horizontal = currentFacing.x;
        }

        if (horizontal < 0f)
        {
            return Vector2.left;
        }
        return Vector2.right;
    }
}
