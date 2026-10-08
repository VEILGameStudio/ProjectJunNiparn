// CameraZone
// One area of a level that has its own camera: a Three Quarter puzzle room or a Side View
// corridor. A scene can hold several zones. Only the zone the player is standing in is
// switched on. The others are switched off, so they cost nothing and can never show up
// in the wrong camera. Build a zone exactly like a small level, then put all of it under
// one child called Content.
//
// Put this on: an empty GameObject named after the zone, for example "Zone_Corridor".
//   This object itself stays switched on all the time.
//     Zone_Corridor     CameraZone + BoxCollider (Is Trigger ticked, layer Ignore Raycast)
//       └─ Content      everything in the zone: floor, walls, props, lights, its
//                       CinemachineCamera and its own LevelSettings
// The box: resize the BoxCollider so it covers everywhere the player can walk in this zone,
//   from wall to wall and from the floor up to the highest place the player can stand.
//   Boxes of two zones must not overlap. Drag this same box into the Play Area of the
//   zone's LevelSettings, so the camera's limits come from it too.
// Assign in Inspector:
//   - Content: the child that holds everything in this zone.
// Also needed: one CameraZoneSwitcher somewhere in the scene, and Default Blend set to Cut
//   on the Main Camera's Cinemachine Brain.

using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CameraZone : MonoBehaviour
{
    [Header("Zone")]
    [Tooltip("The child object that holds everything in this zone: floor, walls, props, lights, its CinemachineCamera and its LevelSettings. It is switched on only while the player is in this zone.")]
    [SerializeField] private GameObject content;

    private BoxCollider zoneBox;
    private LevelSettings levelSettings;

    // This zone's LevelSettings. It sits inside Content.
    public LevelSettings Settings
    {
        get
        {
            if (levelSettings == null && content != null)
            {
                levelSettings = content.GetComponentInChildren<LevelSettings>(true);
            }
            return levelSettings;
        }
    }

    // True while this zone's Content is switched on.
    public bool IsOn => content != null && content.activeSelf;

    // True when a point in the world is inside this zone's box. The point is first measured
    // in the box's own space, so the answer stays right when the zone is moved, turned or scaled.
    public bool Contains(Vector3 worldPosition)
    {
        if (zoneBox == null)
        {
            zoneBox = GetComponent<BoxCollider>();
        }

        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        return IsInsideBox(localPosition, zoneBox.center, zoneBox.size);
    }

    // True when a point is inside a box. Both are measured in the box's own space.
    // Public and static so tests can check it without a scene.
    public static bool IsInsideBox(Vector3 localPosition, Vector3 boxCenter, Vector3 boxSize)
    {
        Vector3 fromCenter = localPosition - boxCenter;
        return Mathf.Abs(fromCenter.x) <= boxSize.x * 0.5f
            && Mathf.Abs(fromCenter.y) <= boxSize.y * 0.5f
            && Mathf.Abs(fromCenter.z) <= boxSize.z * 0.5f;
    }

    // Switches this zone on, then puts its preset onto its camera.
    public void SwitchOn()
    {
        if (content == null)
        {
            return;
        }

        content.SetActive(true);
        if (Settings != null)
        {
            Settings.ApplyToLevelCamera();
        }
    }

    // Switches everything in this zone off.
    public void SwitchOff()
    {
        if (content != null)
        {
            content.SetActive(false);
        }
    }

    // Says so in the Console when this zone is not set up correctly.
    public void CheckSetup()
    {
        if (content == null)
        {
            Debug.LogError($"CameraZone on '{name}': Content is not assigned, so this zone can never be switched on. Make a child named 'Content', put everything of this zone in it, and drag it here.", this);
            return;
        }
        if (content == gameObject)
        {
            Debug.LogError($"CameraZone on '{name}': Content is this same object. Switching it off would switch the zone itself off. Make a child named 'Content', put everything of this zone in it, and drag that child here.", this);
            return;
        }

        int settingsCount = content.GetComponentsInChildren<LevelSettings>(true).Length;
        if (settingsCount != 1)
        {
            Debug.LogError($"CameraZone on '{name}': its Content holds {settingsCount} LevelSettings, but a zone needs exactly one. Put one LevelSettings inside Content and set its Camera Preset.", this);
        }

        if (!GetComponent<BoxCollider>().isTrigger)
        {
            Debug.LogWarning($"CameraZone on '{name}': tick Is Trigger on its BoxCollider, or the player bumps into the edge of the zone.", this);
        }
    }
}
