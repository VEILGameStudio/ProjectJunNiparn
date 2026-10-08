// PlayerMovement
// Moves the player across the floor with a CharacterController. W (or the stick up)
// always walks UP THE SCREEN, which is a diagonal in the 3D world, because the camera
// looks down at an angle. A/D walk left/right on the screen. Holding Run makes the
// player faster. Walls are ordinary colliders: the CharacterController stops at them
// and slides along them by itself. Simple gravity keeps the player on the floor, also
// when walking down steps. Movement only works during normal gameplay (it stops while
// paused, in a dialogue, or in a cutscene).
// It also tells other scripts which way the player is walking (Move Direction in the
// world, Screen Move Direction on the screen). PlayerAnimator uses that to pick the sprite.
//
// Put this on: the Player root (the object at the player's feet). A CharacterController
//   is added automatically. Put the SpriteRenderer on a "Visual" child.
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Walk Speed / Run Speed: how fast the player moves.
//   - Stamina (optional): if assigned, the player can only run while stamina allows it.
//   - Gravity: how fast the player falls when not on the floor.
//   - Camera Transform (optional): leave empty to use the Main Camera.
//   - Visual (optional): the child that holds the SpriteRenderer. It is mirrored
//     (Scale X = -1) to face left. Found automatically if the child is named "Visual".
//     If the Visual has a PlayerAnimator, that script chooses the facing instead and
//     this script leaves the Visual alone.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Speed")]
    [Tooltip("Normal walking speed, in units per second.")]
    [SerializeField] private float walkSpeed = 3f;

    [Tooltip("Speed while the Run button is held, in units per second.")]
    [SerializeField] private float runSpeed = 6f;

    [Tooltip("Optional. If assigned, the player can only run while stamina allows it.")]
    [SerializeField] private PlayerStamina stamina;

    [Header("Gravity")]
    [Tooltip("How fast the player falls when not standing on the floor (a negative number). Keeps the player on the floor when walking down steps.")]
    [SerializeField] private float gravity = -20f;

    [Header("Camera")]
    [Tooltip("The camera that decides which way is 'up the screen'. Leave empty to use the Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Facing")]
    [Tooltip("The child object that holds the SpriteRenderer. It is mirrored (Scale X = -1) to face left. Leave empty to use the child named 'Visual'. If it has a PlayerAnimator, that script chooses the facing instead.")]
    [SerializeField] private Transform visual;

    // A small downward push while standing, so the CharacterController stays on the floor.
    private const float GroundedPush = -2f;

    private CharacterController controller;
    private float verticalSpeed;
    private bool mirrorsVisual;

    // The direction the player is walking in the world (flat on the floor). Zero while standing still.
    public Vector3 MoveDirection { get; private set; }

    // The same direction as seen on the screen: X = right, Y = up. Zero while standing still.
    public Vector2 ScreenMoveDirection { get; private set; }

    // True while the player is moving at run speed.
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (visual == null)
        {
            visual = transform.Find("Visual");
        }
        if (visual == null || visual == transform)
        {
            // Mirroring the root would also mirror the CharacterController, so only a child is allowed.
            Debug.LogError($"PlayerMovement on '{name}': no Visual child found, so the player cannot turn left and right. Create a child named 'Visual', move the SpriteRenderer onto it, and drag it into Visual.", this);
            visual = null;
        }
        if (inputReader == null)
        {
            Debug.LogError($"PlayerMovement on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
        }

        // A PlayerAnimator picks the facing itself. Mirroring here as well would turn the sprite back.
        mirrorsVisual = visual != null && visual.GetComponent<PlayerAnimator>() == null;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (cameraTransform == null)
        {
            Debug.LogError($"PlayerMovement on '{name}': no camera found, so the player cannot tell which way is up the screen. Tag your camera as MainCamera or drag it into Camera Transform.", this);
        }
        if (FindAnyObjectByType<LevelSettings>() == null)
        {
            Debug.LogWarning($"PlayerMovement on '{name}': this scene has no LevelSettings, so the camera is not set up. Add an empty GameObject named 'LevelSettings', add the LevelSettings component, and assign its Level Camera.", this);
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.EnableGameplay();
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.DisableGameplay();
        }
    }

    // Moves the player. A CharacterController is moved in Update, not FixedUpdate.
    private void Update()
    {
        if (inputReader == null || cameraTransform == null || !CanMove())
        {
            StandStill();
            return;
        }

        Vector2 input = inputReader.MoveInput;
        if (input.sqrMagnitude > 0.01f)
        {
            InputDebug.Log($"PlayerMovement '{name}': move={input}", this);
        }

        bool wantsToRun = WantsToRun();
        float speed = wantsToRun ? runSpeed : walkSpeed;

        MoveDirection = CalculateMoveDirection(input, cameraTransform.forward, cameraTransform.right);
        ScreenMoveDirection = CalculateScreenDirection(MoveDirection, cameraTransform.forward, cameraTransform.right);
        IsRunning = wantsToRun && MoveDirection.sqrMagnitude > 0.0001f;

        Vector3 velocity = MoveDirection * speed;
        velocity.y = UpdateVerticalSpeed();

        // The CharacterController stops the player at walls and slides them along.
        controller.Move(velocity * Time.deltaTime);

        if (mirrorsVisual)
        {
            UpdateFacing(input.x);
        }
    }

    // Turns WASD / stick input into a direction on the floor, relative to the camera:
    // W walks along the camera's forward and D along its right, both flattened onto the floor.
    // Public and static so tests can check it without a scene.
    public static Vector3 CalculateMoveDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
    {
        GetFloorAxes(cameraForward, cameraRight, out Vector3 flatForward, out Vector3 flatRight);

        Vector3 direction = flatForward * input.y + flatRight * input.x;
        return Vector3.ClampMagnitude(direction, 1f); // Walking diagonally is not faster.
    }

    // The opposite of CalculateMoveDirection: turns a direction in the world back into a
    // direction on the screen (X = right, Y = up), using the same camera axes. With the
    // camera turned 45 degrees, walking along world +Z is "up and to the right" on screen.
    // Anything that picks a sprite facing must use this, never the world direction itself.
    // Public and static so tests can check it without a scene.
    public static Vector2 CalculateScreenDirection(Vector3 worldDirection, Vector3 cameraForward, Vector3 cameraRight)
    {
        GetFloorAxes(cameraForward, cameraRight, out Vector3 flatForward, out Vector3 flatRight);

        return new Vector2(Vector3.Dot(worldDirection, flatRight), Vector3.Dot(worldDirection, flatForward));
    }

    // The camera's forward and right directions, laid flat on the floor.
    private static void GetFloorAxes(Vector3 cameraForward, Vector3 cameraRight, out Vector3 flatForward, out Vector3 flatRight)
    {
        flatRight = new Vector3(cameraRight.x, 0f, cameraRight.z).normalized;
        flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        // A camera looking straight down has no flat forward, so build it from the right instead.
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.Cross(flatRight, Vector3.up);
        }
    }

    // Moves the player to a new spot at once (spawn points, loading a save). The
    // CharacterController is switched off for a moment, or it would put the player straight back.
    public void TeleportTo(Vector3 position)
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        verticalSpeed = 0f;
    }

    // Clears the "walking" values while the player cannot move (paused, dialogue, cutscene).
    private void StandStill()
    {
        MoveDirection = Vector3.zero;
        ScreenMoveDirection = Vector2.zero;
        IsRunning = false;
    }

    // Pulls the player down while in the air, and keeps them pressed onto the floor while standing.
    private float UpdateVerticalSpeed()
    {
        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = GroundedPush;
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }
        return verticalSpeed;
    }

    // True when the player is holding Run and stamina allows it (if stamina is used).
    private bool WantsToRun()
    {
        return inputReader.RunHeld && (stamina == null || stamina.CanRun);
    }

    // The player can only move during normal gameplay.
    private bool CanMove()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // Mirrors the Visual child so the sprite faces the way the player is walking on screen.
    // (Scale is used instead of SpriteRenderer.flipX, because the world sprite shader does not read flipX.)
    private void UpdateFacing(float horizontal)
    {
        if (visual == null || Mathf.Approximately(horizontal, 0f))
        {
            return;
        }

        Vector3 scale = visual.localScale;
        float facing = horizontal < 0f ? -1f : 1f;
        scale.x = Mathf.Abs(scale.x) * facing;
        visual.localScale = scale;
    }
}
