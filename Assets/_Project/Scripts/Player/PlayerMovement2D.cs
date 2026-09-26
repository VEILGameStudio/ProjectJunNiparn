// PlayerMovement2D
// Moves the player in the 2D side-view scenes. Left/right walks along the ground,
// and up/down moves a little "into" or "out of" the scene (depth), kept between a
// minimum and maximum so the player cannot walk off the floor. Holding Run makes
// the player move faster. Movement only works during normal gameplay (it stops
// while the game is paused or a cutscene is playing).
//
// Put this on: the 2D Player GameObject (it needs a Rigidbody2D and usually a
//   Collider2D and a SpriteRenderer).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Walk Speed / Run Speed: how fast the player moves.
//   - Min Depth / Max Depth: the lowest and highest Y position the player may reach.
//   - Sprite Renderer (optional): used to flip the player to face left or right.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
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

    [Header("Depth Limits (up/down movement)")]
    [Tooltip("The lowest Y position the player is allowed to walk to (closest to the camera).")]
    [SerializeField] private float minDepth = -1f;

    [Tooltip("The highest Y position the player is allowed to walk to (furthest away).")]
    [SerializeField] private float maxDepth = 1f;

    [Header("Facing")]
    [Tooltip("Optional. The SpriteRenderer that is flipped to face the walking direction.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (inputReader == null)
        {
            Debug.LogError($"PlayerMovement2D on '{name}': Input Reader is not assigned. Drag the MainInputReader asset here.", this);
        }
        if (body.gravityScale != 0f)
        {
            // In this side-view style there is no falling, so gravity would pull the player down.
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D Gravity Scale should be 0. Setting it to 0 now.", this);
            body.gravityScale = 0f;
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

    // Moves the player. Uses FixedUpdate because we move a Rigidbody2D.
    private void FixedUpdate()
    {
        if (inputReader == null || !CanMove())
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 input = inputReader.MoveInput;
        if (input.sqrMagnitude > 0.01f)
        {
            InputDebug.Log($"PlayerMovement2D '{name}': move={input}", this);
        }

        float speed = IsRunning() ? runSpeed : walkSpeed;

        Vector2 targetPosition = body.position + input * (speed * Time.fixedDeltaTime);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minDepth, maxDepth);
        body.MovePosition(targetPosition);

        UpdateFacing(input.x);
    }

    // True when the player is holding Run and stamina allows it (if stamina is used).
    private bool IsRunning()
    {
        return inputReader.RunHeld && (stamina == null || stamina.CanRun);
    }

    // The player can only move during normal gameplay.
    private bool CanMove()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
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
