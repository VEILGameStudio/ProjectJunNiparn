// Door
// A door the player can click to travel to another scene. It works for normal
// level-to-level doors AND for doors that switch between 2D and 2.5D mode,
// because each mode is just a different scene. When used, it asks the SceneLoader
// to load the target scene and place the player at the chosen spawn point.
//
// Put this on: the door GameObject (it should also have a Collider so the player
//   can click it, plus the sprite/model).
// Assign in Inspector:
//   - Prompt: the text shown to the player (for example "Enter").
//   - Target Scene Name: the exact scene file name to load (must be in Build Settings).
//   - Target Spawn Point Id: the SpawnPoint id in that scene to arrive at.

using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Short text shown when the player can use this door, e.g. 'Enter'.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Destination")]
    [Tooltip("The exact name of the scene to load. It must be added to Build Settings.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("The id of the SpawnPoint in the target scene where the player will appear.")]
    [SerializeField] private string targetSpawnPointId;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    // Travels to the target scene when the player interacts with the door.
    public void Interact(GameObject interactor)
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("Door: no SceneLoader was found. Make sure the Managers prefab is in the scene.", this);
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"Door on '{name}': Target Scene Name is empty. Type the scene name to load.", this);
            return;
        }

        SceneLoader.Instance.LoadScene(targetSceneName, targetSpawnPointId);
    }
}
