// ParallaxLayer
// Makes a background or foreground layer move at a different speed than the camera
// so the 2D scenes feel like they have depth. Put it on each layer object and set
// one number, the Parallax Factor:
//   0            = the layer does not move at all (fixed in the world).
//   1            = the layer moves exactly with the camera (looks infinitely far).
//   0.4 to 0.6   = far background layers (they drift slower than the world = depth).
//   -0.2 to -0.3 = foreground layers (they drift the opposite way = feel closer).
//
// Put this on: each parallax layer GameObject in a 2D scene (the sky, far hills,
//   a foreground bush, and so on).
// Assign in Inspector:
//   - Parallax Factor: the speed number described above.
//   - Camera Transform (optional): leave empty to use the Main Camera automatically.
//   - Parallax Vertical: tick to also drift up/down (the 2D camera moves a little on Y).

using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax")]
    [Tooltip("0 = does not move, 1 = moves with the camera. Far layers 0.4-0.6, foreground layers -0.2 to -0.3.")]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("The camera this layer reacts to. Leave empty to use the Main Camera automatically.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("If ticked, the layer also drifts up and down, not just left and right.")]
    [SerializeField] private bool parallaxVertical = true;

    // Where the camera was last frame, used to measure how far it moved.
    private Vector3 lastCameraPosition;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning($"ParallaxLayer on '{name}': no camera found. Tag your camera as MainCamera or drag one into Camera Transform.", this);
            return;
        }

        lastCameraPosition = cameraTransform.position;
    }

    // Runs after Cinemachine has moved the camera, so we measure the real movement.
    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        float moveX = cameraDelta.x * parallaxFactor;
        float moveY = parallaxVertical ? cameraDelta.y * parallaxFactor : 0f;
        transform.position += new Vector3(moveX, moveY, 0f);

        lastCameraPosition = cameraTransform.position;
    }
}
