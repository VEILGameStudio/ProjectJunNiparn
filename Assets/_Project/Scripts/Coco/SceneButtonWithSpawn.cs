// SceneButtonWithSpawn
// A "Back" button that loads a scene AND puts the player at a chosen spot in it.
// SceneButtons.GoToScene cannot do this (it gives no spawn point, so the player
// appears at the scene's start, for example in front of the house).
//
// Put this on: any GameObject in the scene with the button (for example the Back button).
// Assign in Inspector: Scene Name, Spawn Point Id.
// Use it: Button > On Click > SceneButtonWithSpawn.Go
// Also needed: in the target scene, an empty object with a SpawnPoint whose
//   Spawn Point Id is the same text as here.

using UnityEngine;

public class SceneButtonWithSpawn : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("The scene to load, for example Test_Forest. It must be in Build Settings.")]
    [SerializeField] private string sceneName;

    [Tooltip("The Spawn Point Id of the place the player should stand in that scene, for example FromBoxPuzzle1.")]
    [SerializeField] private string spawnPointId;

    // Loads the scene behind a fade and moves the player to the spawn point.
    public void Go()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"SceneButtonWithSpawn on '{name}': Scene Name is empty. Type the scene to go back to.", this);
            return;
        }
        if (string.IsNullOrEmpty(spawnPointId))
        {
            Debug.LogWarning($"SceneButtonWithSpawn on '{name}': Spawn Point Id is empty, so the player will appear at the scene's start.", this);
        }
        if (GameManager.Instance == null || GameManager.Instance.SceneLoader == null)
        {
            Debug.LogError($"SceneButtonWithSpawn on '{name}': no GameManager / SceneLoader found. Make sure the Managers object is in the scene.", this);
            return;
        }

        GameManager.Instance.SceneLoader.LoadScene(sceneName, spawnPointId);
    }
}
