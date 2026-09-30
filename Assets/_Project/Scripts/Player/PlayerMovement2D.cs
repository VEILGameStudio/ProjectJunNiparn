// PlayerMovement2D
// Moves the player. A/D (or the stick left/right) walks along X. W/S (or the stick
// up/down) walks deeper into or out of the scene on Y. Up/down uses the Depth Speed
// Multiplier so depth looks right. Holding Run makes the player move faster.
// Walls are ordinary Collider2D objects: the player bumps into them and slides along
// them by physics, so there is no clamping code here. Movement only works during
// normal gameplay (it stops while paused, in a dialogue, or in a cutscene).
//
// Put this on: the Player root (it needs a Rigidbody2D: Dynamic, Gravity Scale 0,
//   Freeze Rotation Z, Collision Detection Continuous - this script fixes any of these
//   that are wrong - plus a small solid Collider2D at the feet, and a Visual child
//   with the SpriteRenderer).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset.
//   - Walk Speed / Run Speed: how fast the player moves.
//   - Depth Speed Multiplier: up/down speed compared to left/right. The scene's
//     LevelSettings sets this when the scene starts (0.55 side view, 0.85 three-quarter).
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

    [Tooltip("Up/down speed compared to left/right speed. The scene's LevelSettings replaces this when the scene starts.")]
    [SerializeField] private float depthSpeedMultiplier = 0.55f;

    [Tooltip("Optional. If assigned, the player can only run while stamina allows it.")]
    [SerializeField] private PlayerStamina stamina;

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

        ApplyRequiredPhysicsSettings();
    }

    private void Start()
    {
        if (FindAnyObjectByType<LevelSettings>() == null)
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': this scene has no LevelSettings, so default values are used. Add an empty GameObject named 'LevelSettings', add the LevelSettings component, and pick a View Style.", this);
        }
    }

    // Makes sure the Rigidbody2D is set up the way walking needs, fixing it with a warning if not.
    private void ApplyRequiredPhysicsSettings()
    {
        if (body.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D Body Type should be Dynamic, or the player walks through walls. Setting it now.", this);
            body.bodyType = RigidbodyType2D.Dynamic;
        }
        if (body.gravityScale != 0f)
        {
            // There is no falling in this game, so gravity would pull the player down.
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D Gravity Scale should be 0. Setting it now.", this);
            body.gravityScale = 0f;
        }
        if (!body.freezeRotation)
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D should tick Freeze Rotation Z, or the player tips over when bumping walls at an angle. Setting it now.", this);
            body.freezeRotation = true;
        }
        if (body.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
        {
            Debug.LogWarning($"PlayerMovement2D on '{name}': Rigidbody2D Collision Detection should be Continuous, or a fast player can slip through thin walls. Setting it now.", this);
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

    // Sets how fast the player walks up/down compared to left/right. LevelSettings calls this.
    public void SetDepthSpeedMultiplier(float value)
    {
        depthSpeedMultiplier = value;
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

        // Physics stops the player at walls and slides them along.
        body.MovePosition(body.position + step);

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
