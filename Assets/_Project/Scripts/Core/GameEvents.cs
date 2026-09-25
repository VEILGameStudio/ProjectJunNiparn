// GameEvents
// A central place for game-wide announcements ("events"). One system raises an
// event, and any other system can listen for it, without the two needing to know
// about each other. This keeps scripts loosely connected and easy to change.
//
// How to listen. ALWAYS subscribe in OnEnable and unsubscribe in OnDisable:
//   private void OnEnable()  { GameEvents.OnGameOver += HandleGameOver; }
//   private void OnDisable() { GameEvents.OnGameOver -= HandleGameOver; }
//   private void HandleGameOver() { ... }
//
// Only core systems raise these events (with the Raise... methods below).
// Gameplay scripts may listen, but must never raise them.
//
// Put this on: nothing. It is a static helper used from other scripts.

using System;
using System.Collections.Generic;

public static class GameEvents
{
    // The inventory changed. Sends the current slots (read only).
    public static event Action<IReadOnlyList<InventorySlot>> OnInventoryChanged;

    // The player's health changed. Sends (current health, max health).
    public static event Action<int, int> OnHealthChanged;

    // The player's stamina changed. Sends (current stamina, max stamina).
    public static event Action<float, float> OnStaminaChanged;

    // The player picked up an item. Sends (item, amount).
    public static event Action<ItemData, int> OnItemPickedUp;

    // A full or choice dialogue opened. Player input is locked until it ends.
    public static event Action OnDialogueStarted;

    // The dialogue closed. Player input is released.
    public static event Action OnDialogueEnded;

    // A puzzle was solved. Sends the puzzle id.
    public static event Action<string> OnPuzzleCompleted;

    // The player failed (health reached 0 or a puzzle timer ran out).
    public static event Action OnGameOver;

    // A new scene finished loading and the player is standing at the spawn point.
    public static event Action OnSceneReady;

    // The player switched language in Settings. Text on screen should refresh.
    public static event Action OnLanguageChanged;

    // The methods below are called by core systems to raise each event.

    public static void RaiseInventoryChanged(IReadOnlyList<InventorySlot> slots) => OnInventoryChanged?.Invoke(slots);
    public static void RaiseHealthChanged(int currentHealth, int maxHealth) => OnHealthChanged?.Invoke(currentHealth, maxHealth);
    public static void RaiseStaminaChanged(float currentStamina, float maxStamina) => OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    public static void RaiseItemPickedUp(ItemData item, int amount) => OnItemPickedUp?.Invoke(item, amount);
    public static void RaiseDialogueStarted() => OnDialogueStarted?.Invoke();
    public static void RaiseDialogueEnded() => OnDialogueEnded?.Invoke();
    public static void RaisePuzzleCompleted(string puzzleId) => OnPuzzleCompleted?.Invoke(puzzleId);
    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseSceneReady() => OnSceneReady?.Invoke();
    public static void RaiseLanguageChanged() => OnLanguageChanged?.Invoke();
}
