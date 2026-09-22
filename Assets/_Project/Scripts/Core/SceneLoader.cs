// SceneLoader
// Changes from one scene to another the smooth way: fade to black, (optionally)
// autosave, load the new scene, place the player at the correct spawn point, then
// fade back in. This is used by doors, including doors that switch between the 2D
// and 2.5D modes (each mode is just a different scene).
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector:
//   - Screen Fader: the ScreenFader on the persistent fade Canvas.
//   - Player Tag: the tag on your player object (default "Player").
//   - Autosave On Load: tick to autosave every time a new scene is entered.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    [Header("References")]
    [Tooltip("The ScreenFader used to fade the screen in and out during a scene change.")]
    [SerializeField] private ScreenFader screenFader;

    [Header("Settings")]
    [Tooltip("The tag on your player GameObject. The loader moves this object to the spawn point.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If ticked, the game autosaves each time a new scene finishes loading.")]
    [SerializeField] private bool autosaveOnLoad = true;

    // The spawn point the player should appear at in the scene being loaded.
    private string pendingSpawnPointId;

    // True while a scene change is happening, to avoid starting two at once.
    private bool isLoading;

    // Loads a scene and places the player at the spawn point with this id.
    public void LoadScene(string sceneName, string spawnPointId)
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: LoadScene was called with an empty scene name. Check the door's Target Scene.");
            return;
        }

        pendingSpawnPointId = spawnPointId;
        StartCoroutine(LoadRoutine(sceneName));
    }

    // Handles the whole fade-out, load, place-player, fade-in sequence.
    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;
        GameEvents.RaiseSceneLoadStarted();

        if (screenFader != null)
        {
            yield return screenFader.FadeOut();
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        PlacePlayerAtSpawnPoint();

        if (autosaveOnLoad && SaveManager.Instance != null && !SaveManager.Instance.IsRestoring)
        {
            SaveManager.Instance.SaveGame();
        }

        GameEvents.RaiseSceneLoadFinished();

        if (screenFader != null)
        {
            yield return screenFader.FadeIn();
        }

        isLoading = false;
    }

    // Finds the spawn point with the pending id and moves the player onto it.
    private void PlacePlayerAtSpawnPoint()
    {
        if (string.IsNullOrEmpty(pendingSpawnPointId))
        {
            return; // No spawn point requested (for example the very first scene).
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning($"SceneLoader: no object tagged '{playerTag}' was found in '{SceneManager.GetActiveScene().name}', so the player could not be placed.");
            return;
        }

        SpawnPoint target = FindSpawnPoint(pendingSpawnPointId);
        if (target == null)
        {
            Debug.LogWarning($"SceneLoader: no SpawnPoint with id '{pendingSpawnPointId}' was found in the loaded scene.");
            return;
        }

        player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
    }

    // Looks through the loaded scene for a spawn point with the matching id.
    private SpawnPoint FindSpawnPoint(string spawnPointId)
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnPointId == spawnPointId)
            {
                return spawnPoint;
            }
        }
        return null;
    }
}
