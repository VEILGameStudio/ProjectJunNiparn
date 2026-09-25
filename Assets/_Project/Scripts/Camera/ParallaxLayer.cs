// ParallaxLayer
// Makes a background or foreground layer move at a different speed than the camera
// so the scene feels like it has depth. Each layer has an X and a Y factor:
//   0            = the layer does not move at all (fixed in the world).
//   1            = the layer moves exactly with the camera (looks infinitely far).
//   0.4 to 0.6   = far background layers on X (they drift slower than the world = depth).
//   -0.2 to -0.3 = foreground layers on X (they drift the opposite way = feel closer).
// Set the Y factor to about half the X factor. Use 0 on Y to stop up/down drift.
//
// Put this on: each parallax layer GameObject (the sky, far hills, a foreground
//   bush, and so on).
// Assign in Inspector:
//   - Parallax Factor X / Y: the speed numbers described above.
//   - Camera Transform (optional): leave empty to use the Main Camera automatically.

using UnityEngine;
using UnityEngine.Serialization;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax")]
    [Tooltip("Left/right speed. 0 = does not move, 1 = moves with the camera. Far layers 0.4-0.6, foreground layers -0.2 to -0.3.")]
    [FormerlySerializedAs("parallaxFactor")]
    [SerializeField] private float parallaxFactorX = 0.5f;

    [Tooltip("Up/down speed. Usually about half of Parallax Factor X. Use 0 to stop up/down drift.")]
    [SerializeField] private float parallaxFactorY = 0.25f;

    [Tooltip("The camera this layer reacts to. Leave empty to use the Main Camera automatically.")]
    [SerializeField] private Transform cameraTransform;

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

        float moveX = cameraDelta.x * parallaxFactorX;
        float moveY = cameraDelta.y * parallaxFactorY;
        transform.position += new Vector3(moveX, moveY, 0f);

        lastCameraPosition = cameraTransform.position;
    }
}
