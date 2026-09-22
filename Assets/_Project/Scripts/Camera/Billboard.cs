// Billboard
// Keeps a flat 2D sprite turned to face the camera so it always looks correct in
// a 3D scene. Use it in the 2.5D scenes on the player's sprite, and on any other
// sprite (items, enemies) that should face the camera.
//
// Put this on: the child GameObject that holds the SpriteRenderer.
// Assign in Inspector:
//   - Target Camera (optional): leave empty to use the main camera automatically.
//   - Stay Upright: keep the sprite standing straight (recommended for characters).

using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Header("Billboard")]
    [Tooltip("The camera to face. Leave empty to use the Main Camera automatically.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("If ticked, the sprite only turns sideways and never tilts up or down. Best for characters.")]
    [SerializeField] private bool stayUpright = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        if (targetCamera == null)
        {
            Debug.LogWarning($"Billboard on '{name}': no camera found. Tag your camera as MainCamera or assign Target Camera.", this);
        }
    }

    // Turns to face the camera after all movement is done, to avoid jitter.
    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (stayUpright)
        {
            // Face the same direction the camera looks, but stay standing straight up.
            Vector3 lookDirection = targetCamera.transform.forward;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        else
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }
}
