// SpawnPoint
// Marks a place in a scene where the player should appear after walking through a
// door. Each door says which Spawn Point Id to use, and the SceneLoader moves the
// player to the matching SpawnPoint in the newly loaded scene.
//
// Put this on: an empty GameObject placed where the player should stand.
// Assign in Inspector: "Spawn Point Id" (a short name that a door will point to,
//   for example "FromLevel2" or "MainEntrance").

using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("A unique name for this spawn point. A door in another scene points to this name.")]
    [SerializeField] private string spawnPointId = "Default";

    // The name doors use to find this spawn point.
    public string SpawnPointId => spawnPointId;

    // Draws a small marker in the Scene view so it is easy to place.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
