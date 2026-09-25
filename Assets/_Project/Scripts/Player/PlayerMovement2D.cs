// PlayerMovement2D
// Moves the player. A/D (or the stick left/right) walks along X. W/S (or the stick
// up/down) walks deeper into or out of the scene on Y. Up/down is slower (the
// Depth Speed Multiplier) so depth looks right. Holding Run makes the player move
// faster. The player can never leave the Walkable Area. Movement only works during
// normal gameplay (it stops while paused, in a dialogue, or in a cutscene).
//
// Put this on: the Player GameObject (it needs a Rigidbody2D, and usually a
//   Collider2D and a SpriteRenderer).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Walk Speed / Run Speed: how fast the player moves.
//   - Depth Speed Multiplier: up/down speed compared to left/right (default 0.55).
//   - Walkable Area: this level's floor outline (a PolygonCollider2D with Is Trigger ticked).
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

    [Tooltip("Up/down speed compared to left/right speed. 0.55 makes walking into the scene look right.")]
    [SerializeField] private float depthSpeedMultiplier = 0.55f;

    [Tooltip("Optional. If assigned, the player can only run while stamina allows it.")]
    [SerializeField] private PlayerStamina stamina;

    [Header("Walkable Area")]
    [Tooltip("The PolygonCollider2D that outlines the floor of this level. Tick 'Is Trigger' on it. The player cannot walk outside it.")]
    [SerializeField] private PolygonCollider2D walkableArea;

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
        if (walkableArea == null)
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': Walkable Area is not assigned, so the player can walk anywhere. Drag this level's floor PolygonCollider2D here.", this);
        }
        if (body.gravityScale != 0f)
        {
            // In this side-view style there is no falling, so gravity would pull the player down.
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D Gravity Scale should be 0. Setting it to 0 now.", this);
            body.gravityScale = 0f;
        }
    }

    private void Start()
    {
        if (walkableArea != null && !walkableArea.OverlapPoint(body.position))
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': the player starts outside the Walkable Area and will not be able to move. Move the player (or the spawn point) inside the floor outline.", this);
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
        Vector2 step = new Vector2(input.x, input.y * depthSpeedMultiplier) * (speed * Time.fixedDeltaTime);

        body.MovePosition(KeepInsideWalkableArea(body.position, step));

        UpdateFacing(input.x);
    }

    // Returns where the player may move without leaving the Walkable Area. If the
    // full step would leave it, the player slides along X only, then Y only.
    private Vector2 KeepInsideWalkableArea(Vector2 from, Vector2 step)
    {
        Vector2 fullStep = from + step;
        if (walkableArea == null || walkableArea.OverlapPoint(fullStep))
        {
            return fullStep;
        }

        Vector2 sideStep = from + new Vector2(step.x, 0f);
        if (walkableArea.OverlapPoint(sideStep))
        {
            return sideStep;
        }

        Vector2 depthStep = from + new Vector2(0f, step.y);
        if (walkableArea.OverlapPoint(depthStep))
        {
            return depthStep;
        }

        return from;
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
