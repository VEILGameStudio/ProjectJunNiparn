// GameManager
// The "brain" of the game. It remembers the current game state (Playing, Paused,
// Cutscene, GameOver) and is the single place other scripts check to ask
// "can the player move right now?".
// It also handles pausing: when paused it freezes time so timers and movement stop.
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector: nothing required.

using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Read Only (for checking in the editor)")]
    [Tooltip("The current state of the game. Set automatically at runtime.")]
    [SerializeField] private GameState currentState = GameState.Playing;

    // The current game state. Other scripts read this to decide what to do.
    public GameState CurrentState => currentState;

    // True only during normal gameplay. Player movement scripts should check this.
    public bool IsPlaying => currentState == GameState.Playing;

    // Changes the game state and tells the rest of the game about it.
    public void SetState(GameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        GameEvents.RaiseGameStateChanged(newState);
    }

    // Opens a menu/inventory: freezes time and switches to the Paused state.
    // UI must use unscaled time so buttons still work while time is frozen.
    public void Pause()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        SetState(GameState.Paused);
        GameEvents.RaiseGamePaused();
    }

    // Closes the menu/inventory: unfreezes time and returns to normal play.
    public void Resume()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Playing);
        GameEvents.RaiseGameResumed();
    }
}
