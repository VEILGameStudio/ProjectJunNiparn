// DepthScaler
// Makes a character look a little smaller as it walks deeper into the scene (higher
// on the screen), and full size at the front of the floor. It scales ONLY the Visual
// child that holds the SpriteRenderer, never this root object: the root carries the
// Rigidbody2D and Collider2D, and scaling those would make bumping and clicking
// change with depth.
//
//   Player            Rigidbody2D + Collider2D   (never scaled)
//     └─ Visual       SpriteRenderer             (scaled by depth)
//
// The scene's LevelSettings turns this on or off and tells it where the floor starts
// and ends (its Depth Range). Without a LevelSettings the character stays full size.
//
// Put this on: the Player root (or any walking character built the same way).
// Assign in Inspector:
//   - Visual: the child with the SpriteRenderer (found automatically if it is named "Visual").
//   - Near Scale / Far Scale: size at the front and at the back of the floor (1.0 and 0.85).

using UnityEngine;

public class DepthScaler : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("The child object that holds the SpriteRenderer. Never this object itself. Found automatically if it is named 'Visual'.")]
    [SerializeField] private Transform visual;

    [Header("Scale")]
    [Tooltip("Size when standing at the front edge of the floor (nearest the camera).")]
    [SerializeField] private float nearScale = 1f;

    [Tooltip("Size when standing at the back edge of the floor (furthest away).")]
    [SerializeField] private float farScale = 0.85f;

    private bool isScaling;
    private float nearY;
    private float farY;

    private void Awake()
    {
        if (visual == null)
        {
            visual = transform.Find("Visual");
        }

        if (visual == null)
        {
            Debug.LogError($"DepthScaler on '{name}': no Visual child found. Make a child object named 'Visual', move the SpriteRenderer onto it, and drag it into Visual.", this);
        }
        else if (visual == transform)
        {
            Debug.LogError($"DepthScaler on '{name}': Visual must be a child object, not '{name}' itself. Scaling the root would also scale its colliders.", this);
            visual = null;
        }
    }

    // Turns depth scaling on or off and sets the floor's front and back Y. LevelSettings calls this.
    public void Configure(bool scalingOn, float floorNearY, float floorFarY)
    {
        isScaling = scalingOn;
        nearY = floorNearY;
        farY = floorFarY;

        if (!isScaling)
        {
            SetVisualScale(1f);
        }
    }

    // Updates the size after the character has moved this frame.
    private void LateUpdate()
    {
        if (!isScaling || visual == null)
        {
            return;
        }

        SetVisualScale(CalculateScale(transform.position.y, nearY, farY, nearScale, farScale));
    }

    // Works out the size for a Y position between the floor's front (nearY) and back (farY).
    public static float CalculateScale(float y, float nearY, float farY, float nearScale, float farScale)
    {
        float depth = Mathf.InverseLerp(nearY, farY, y); // 0 at the front edge, 1 at the back edge
        return Mathf.Lerp(nearScale, farScale, depth);
    }

    private void SetVisualScale(float scale)
    {
        if (visual != null)
        {
            visual.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
