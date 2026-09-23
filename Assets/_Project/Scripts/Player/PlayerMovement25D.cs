// PlayerMovement25D
// Moves the player in the 2.5D top-down scenes. The player is a flat 2D sprite
// standing in a 3D world, seen from a fixed 3/4 angle camera. W/A/S/D (or the
// gamepad stick) walk across the ground plane using 3D physics. Holding Run makes
// the player move faster. A little gravity keeps the player on the ground.
// Movement only works during normal gameplay.
//
// The sprite is kept facing the camera by a separate Billboard script placed on
// the sprite child object.
//
// Put this on: the 2.5D Player GameObject (it needs a CharacterController).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Walk Speed / Run Speed: how fast the player moves.
//   - Gravity: downward pull that keeps the player grounded (a negative number).
//   - Sprite Renderer (optional): used to flip the player to face left or right.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement25D : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here.")]
    [SerializeField] private InputReader inputReader;

    [Header("Speed")]
    [Tooltip("Normal walking speed, in units per second.")]
    [SerializeField] private float walkSpeed = 3.5f;

    [Tooltip("Speed while the Run button is held, in units per second.")]
    [SerializeField] private float runSpeed = 7f;

    [Tooltip("Optional. If assigned, the player can only run while stamina allows it.")]
    [SerializeField] private PlayerStamina stamina;

    [Header("Gravity")]
    [Tooltip("Downward pull that keeps the player on the ground. Use a negative number.")]
    [SerializeField] private float gravity = -20f;

    [Header("Facing")]
    [Tooltip("Optional. The SpriteRenderer that is flipped to face the walking direction.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private CharacterController controller;

    // How fast the player is currently falling (built up by gravity each frame).
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (inputReader == null)
        {
            Debug.LogError($"PlayerMovement25D on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
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

    // Moves the player every frame using the CharacterController.
    private void Update()
    {
        if (inputReader == null)
        {
            return;
        }

        // Left/right becomes world X, up/down becomes world Z (forward on the ground).
        Vector2 input = CanMove() ? inputReader.MoveInput : Vector2.zero;
        Vector3 horizontalMove = new Vector3(input.x, 0f, input.y);
        if (horizontalMove.sqrMagnitude > 1f)
        {
            horizontalMove.Normalize(); // Stops diagonal movement from being faster.
        }

        float speed = IsRunning() ? runSpeed : walkSpeed;
        ApplyGravity();

        Vector3 motion = horizontalMove * speed + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        UpdateFacing(input.x);
    }

    // The player can only move during normal gameplay.
    private bool CanMove()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // True when the player is holding Run and stamina allows it (if stamina is used).
    private bool IsRunning()
    {
        return inputReader.RunHeld && (stamina == null || stamina.CanRun);
    }

    // Builds up downward speed, but presses the player gently onto the ground when grounded.
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    // Flips the sprite so it faces the way the player is walking.
    private void UpdateFacing(float horizontal)
    {
        if (spriteRenderer == null || Mathf.Approximately(horizontal, 0f))
        {
            return;
        }

        spriteRenderer.flipX = horizontal < 0f;
    }
}
