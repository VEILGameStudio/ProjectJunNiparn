// CameraZoneSwitcher
// Keeps exactly one CameraZone switched on: the one the player is standing in. Every
// frame it checks which zone box the player is inside. When that changes (the player
// went through a ZoneDoor, a save was loaded, a spawn point was used) it switches the
// old zone off and the new one on, turns the sprites to the new camera angle, and tells
// the rest of the game with GameEvents.OnCameraZoneChanged.
// If the player ends up outside every zone box, the zone that is on simply stays on.
// It never switches every zone off: that would leave a black screen.
//
// Put this on: one empty GameObject per scene that uses camera zones, for example the
//   "Zones" parent. A scene without zones does not need it.
// Assign in Inspector:
//   - Player Tag: the tag on the player object (default "Player").
// Also needed: select the Main Camera and set Cinemachine Brain > Default Blend to Cut.

using Unity.Cinemachine;
using UnityEngine;

// Runs before the player moves, so the floor of a new zone is switched on before the player stands on it.
[DefaultExecutionOrder(-50)]
public class CameraZoneSwitcher : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("The tag on the player object.")]
    [SerializeField] private string playerTag = "Player";

    private CameraZone[] zones;
    private CameraZone currentZone;
    private Transform player;

    private void Awake()
    {
        zones = FindObjectsByType<CameraZone>();
        if (zones.Length == 0)
        {
            Debug.LogError($"CameraZoneSwitcher on '{name}': this scene has no CameraZone, so there is nothing to switch. Add CameraZone objects, or remove this component.", this);
            return;
        }

        foreach (CameraZone zone in zones)
        {
            zone.CheckSetup();
        }

        FindPlayer();
        SwitchOnStartZone();
    }

    private void Start()
    {
        CheckBrainCuts();
    }

    // Switches to the zone the player walked, was moved, or was loaded into.
    private void Update()
    {
        if (zones == null || zones.Length == 0)
        {
            return;
        }
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                return;
            }
        }

        if (currentZone != null && currentZone.Contains(player.position))
        {
            return; // The usual case: the player is still in the same zone.
        }

        CameraZone zone = FindZoneAt(player.position);
        if (zone == null || zone == currentZone)
        {
            return; // Outside every zone: the current zone stays on.
        }

        SwitchTo(zone);
    }

    // Switches from the current zone to another one. The order matters:
    // the camera must have its new angle before the sprites turn to face it.
    private void SwitchTo(CameraZone zone)
    {
        if (currentZone != null)
        {
            currentZone.SwitchOff();
        }

        currentZone = zone;
        zone.SwitchOn();              // Content on, then the preset is put onto its camera.
        SpriteBillboard.RefreshAll(); // Sprites turn to the new camera angle.

        if (zone.Settings != null)
        {
            GameEvents.RaiseCameraZoneChanged(zone.Settings.Preset);
        }
    }

    // When the scene starts: switches on the zone the player stands in and switches the others off.
    private void SwitchOnStartZone()
    {
        CameraZone startZone = null;
        if (player != null)
        {
            startZone = FindZoneAt(player.position);
        }

        if (startZone == null)
        {
            startZone = FindZoneThatIsOn();
            Debug.LogError($"CameraZoneSwitcher on '{name}': the player is not inside any CameraZone box, so '{startZone.name}' was switched on instead. Move the Player inside a zone's box, or make that box bigger.", this);
        }

        foreach (CameraZone zone in zones)
        {
            if (zone != startZone)
            {
                zone.SwitchOff();
            }
        }

        currentZone = startZone;
        startZone.SwitchOn();
    }

    // The zone whose box holds this point, or null if no zone does.
    private CameraZone FindZoneAt(Vector3 worldPosition)
    {
        foreach (CameraZone zone in zones)
        {
            if (zone.Contains(worldPosition))
            {
                return zone;
            }
        }
        return null;
    }

    // A zone that is already switched on in the scene, or else the first one found.
    private CameraZone FindZoneThatIsOn()
    {
        foreach (CameraZone zone in zones)
        {
            if (zone.IsOn)
            {
                return zone;
            }
        }
        return zones[0];
    }

    private void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found != null)
        {
            player = found.transform;
        }
    }

    // An orthographic and a perspective camera cannot be blended, so the Brain has to cut between zones.
    private void CheckBrainCuts()
    {
        CinemachineBrain brain = FindAnyObjectByType<CinemachineBrain>();
        if (brain != null && brain.DefaultBlend.Style != CinemachineBlendDefinition.Styles.Cut)
        {
            Debug.LogError($"CameraZoneSwitcher on '{name}': the Cinemachine Brain blends between cameras, which makes the picture jump when the camera zone changes. Select the Main Camera, find Cinemachine Brain, and set Default Blend to Cut.", this);
        }
    }
}
