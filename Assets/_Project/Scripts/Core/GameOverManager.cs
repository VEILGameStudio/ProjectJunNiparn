// GameOverManager
// The shared "you failed" sequence, used both when the player dies (PlayerHealth)
// and when a puzzle timer runs out (PuzzleTimer). It fades the screen to black,
// waits a short delay you set in the Inspector, then loads the last save so the
// player continues from their most recent checkpoint.
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector:
//   - Screen Fader: the ScreenFader on the persistent fade Canvas (same one the
//     SceneLoader uses).
//   - Delay Seconds: how long the screen stays black before reloading (2 to 5).

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : Singleton<GameOverManager>
{
    [Header("References")]
    [Tooltip("The ScreenFader used to fade the screen to black.")]
    [SerializeField] private ScreenFader screenFader;

    [Header("Timing")]
    [Range(2f, 5f)]
    [Tooltip("How long the screen stays black before loading the last save.")]
    [SerializeField] private float delaySeconds = 3f;

    private bool isGameOver;

    // Starts the game over sequence. Safe to call more than once; it only runs once.
    public void TriggerGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.GameOver);
        }

        StartCoroutine(GameOverRoutine());
    }

    // Fades to black, waits, then loads the last save (or restarts the scene).
    private IEnumerator GameOverRoutine()
    {
        if (screenFader != null)
        {
            yield return screenFader.FadeOut();
        }

        // Real time, so the wait works even if the game was frozen.
        yield return new WaitForSecondsRealtime(delaySeconds);

        // Back to normal so the reloaded game plays.
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Playing);
        }

        isGameOver = false;

        bool loaded = SaveManager.Instance != null && SaveManager.Instance.ContinueGame();
        if (!loaded)
        {
            // No save yet: just restart the current scene so the player can retry.
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(SceneManager.GetActiveScene().name, null);
            }
        }
    }
}
