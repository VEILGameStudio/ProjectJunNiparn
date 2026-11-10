// PauseManager
// Freezes the game when a menu or the inventory is open, and unfreezes it when
// everything is closed again. It sets Time.timeScale to 0 (so gameplay, movement,
// and timers stop) but UI still works because UI runs on unscaled time.
//
// It counts how many things asked for a pause, so if BOTH the menu and the
// inventory are open, closing just one keeps the game frozen until the last one
// closes. Each opener passes itself as the "source":
//   GameManager.Instance.PauseManager.Pause(this);   // when a menu/inventory opens
//   GameManager.Instance.PauseManager.Resume(this);  // when it closes
//
// Put this on: the "Managers" GameObject (next to the GameManager).
// Assign in Inspector: nothing required.

using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // The things currently asking the game to stay paused (menu, inventory, ...).
    private readonly HashSet<object> pauseSources = new HashSet<object>();

    // True while the game is frozen.
    public bool IsPaused => pauseSources.Count > 0;

    // Freezes the game because this source (a menu or inventory) opened.
    public void Pause(object source)
    {
        if (source == null)
        {
            return;
        }

        bool wasEmpty = pauseSources.Count == 0;
        pauseSources.Add(source);

        // Only freeze on the first opener.
        if (wasEmpty && pauseSources.Count > 0)
        {
            Time.timeScale = 0f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Paused);
            }
        }
    }

    // Removes this source; unfreezes only when nothing else is still open.
    public void Resume(object source)
    {
        if (source == null || !pauseSources.Remove(source))
        {
            return;
        }

        if (pauseSources.Count == 0)
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }
    }
}
