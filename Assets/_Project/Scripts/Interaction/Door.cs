// Door
// A door that takes the player to another scene. When used, it asks the
// SceneLoader to load the target scene and place the player at the chosen spawn
// point. To lock it, set Required Item and Blocked Message (from InteractableBase).
//
// Put this on: the door GameObject. Add a Collider2D first (for example a BoxCollider2D).
// Assign in Inspector:
//   - Target Scene Name: the exact scene file name to load (must be in Build Settings).
//   - Target Spawn Point Id: the SpawnPoint id in that scene to arrive at.
//   - Activation / Max Click Distance / Required Item / Blocked Message: see InteractableBase.

using UnityEngine;

public class Door : InteractableBase
{
    [Header("Destination")]
    [Tooltip("The exact name of the scene to load. It must be added to Build Settings.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("The id of the SpawnPoint in the target scene where the player will appear.")]
    [SerializeField] private string targetSpawnPointId;

    // Travels to the target scene.
    protected override void OnInteract(PlayerContext player)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"Door on '{name}': Target Scene Name is empty. Type the scene name to load.", this);
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.SceneLoader == null)
        {
            Debug.LogError($"Door on '{name}': no SceneLoader was found. Make sure the Managers object (with a SceneLoader) is in the scene.", this);
            return;
        }

        GameManager.Instance.SceneLoader.LoadScene(targetSceneName, targetSpawnPointId);
    }
}
