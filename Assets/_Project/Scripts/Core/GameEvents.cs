// GameEvents
// A central place for game-wide announcements ("events"). One system raises an
// event, and any other system can listen for it, without the two needing to know
// about each other. This keeps scripts loosely connected and easy to change.
//
// Example - a script that wants to react when the language changes:
//   void OnEnable()  { GameEvents.LanguageChanged += RefreshText; }
//   void OnDisable() { GameEvents.LanguageChanged -= RefreshText; }
//   void RefreshText() { ... }
//
// Always unsubscribe (-=) in OnDisable so destroyed objects are not called.
//
// Put this on: nothing. It is a static helper used from other scripts.

using System;

public static class GameEvents
{
    // The game state changed (Playing / Paused / Cutscene / GameOver).
    public static event Action<GameState> GameStateChanged;

    // The player opened a menu or inventory and the game paused.
    public static event Action GamePaused;

    // The game resumed normal play.
    public static event Action GameResumed;

    // The player switched language in Settings.
    public static event Action LanguageChanged;

    // A scene load began (use this to start a fade-out).
    public static event Action SceneLoadStarted;

    // A scene finished loading (use this to fade back in).
    public static event Action SceneLoadFinished;

    // The methods below are called by the manager scripts to raise each event.

    public static void RaiseGameStateChanged(GameState newState) => GameStateChanged?.Invoke(newState);
    public static void RaiseGamePaused() => GamePaused?.Invoke();
    public static void RaiseGameResumed() => GameResumed?.Invoke();
    public static void RaiseLanguageChanged() => LanguageChanged?.Invoke();
    public static void RaiseSceneLoadStarted() => SceneLoadStarted?.Invoke();
    public static void RaiseSceneLoadFinished() => SceneLoadFinished?.Invoke();
}
