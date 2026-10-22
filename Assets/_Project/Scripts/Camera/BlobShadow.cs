// BlobShadow
// A soft dark spot on the floor under a character or prop, so it looks like it stands on
// the ground. (Sprites cannot cast real shadows: a flat card would cast a card-shaped one.)
// The spot is a flat Quad lying on the floor. This script lays it down when the level
// starts and when the camera zone changes.
// Tilt Angle lifts the far edge of the spot toward the camera. Leave it at 0. It is there
// for Side View areas, where the camera looks almost along the floor and a flat spot is
// close to invisible: a higher Tilt Angle shows more of it. If a character looks like it
// is floating, try Tilt Angle 45 before changing anything else.
//
// Put this on: a Quad child of the character or prop, at its feet and just above the
//   floor (Y = 0.02), with the BlobShadow material. Put it on the root, not on "Visual",
//   or it turns and mirrors with the sprite. Switch Cast Shadows off on its Mesh Renderer
//   and remove its Mesh Collider.
// Assign in Inspector:
//   - Tilt Angle: leave at 0 in the normal case.
// Its size is the Quad's Scale: X = width, Y = depth on the floor.

using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    [Header("Tilt (leave at 0)")]
    [Tooltip("Lifts the far edge of the shadow toward the camera, in degrees. 0 = lies flat on the floor (normal). Raise it only in Side View areas where the flat shadow cannot be seen: 45 shows most of it.")]
    [Range(0f, 80f)]
    [SerializeField] private float tiltAngle;

    private void OnEnable()
    {
        GameEvents.OnCameraZoneChanged += HandleCameraZoneChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnCameraZoneChanged -= HandleCameraZoneChanged;
    }

    private void Start()
    {
        Refresh();
    }

    // Lays the shadow on the floor, turned toward the camera. Call it again if the camera angle changes.
    public void Refresh()
    {
        if (!SpriteBillboard.TryGetCameraYaw(out float cameraYaw))
        {
            cameraYaw = 0f;
        }

        transform.rotation = CalculateRotation(cameraYaw, tiltAngle);
    }

    // The rotation of a shadow Quad: flat on the floor, with its far edge lifted by the tilt.
    // Public and static so tests can check it without a scene.
    public static Quaternion CalculateRotation(float cameraYaw, float tiltAngle)
    {
        return Quaternion.Euler(90f - tiltAngle, cameraYaw, 0f);
    }

    private void HandleCameraZoneChanged(CameraPreset preset)
    {
        Refresh();
    }
}
