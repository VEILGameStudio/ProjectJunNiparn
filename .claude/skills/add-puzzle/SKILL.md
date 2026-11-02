---
name: add-puzzle
description: Use when a teammate wants a new puzzle, mini game or quick-time event, or wants to change how an existing puzzle starts, completes, fails or unlocks something.
---

# Add or change a puzzle

Spec: CLAUDE.md §8 (`PuzzleBase`, `MiniGameBase`, `QuickTimeEvent`, held-item checks) and §12.

## 1. Shape of a level
About five puzzles per level: one large, four small. Small ones may stand alone or together unlock the
large one through `requiredPuzzleIds`.

## 2. Ladder
1. A new QTE → configure `QuickTimeEvent` in the Inspector. Should need no code.
2. Gating, doors, sounds, animations → `requiredPuzzleIds` + `OnPuzzleStarted/Completed/Failed`
   UnityEvents. (Intern B)
3. Item in hand inside a puzzle screen → `HeldItemCheck` component, call `Check()` from a button. (Intern B)
4. New mechanic → subclass `PuzzleBase` (or `MiniGameBase`) in `Scripts/Gameplay/Puzzles/` or
   `MiniGames/`. Call `StartPuzzle()`, `CompletePuzzle()`, `FailPuzzle()`; nothing else talks to save. (Intern A)

## 3. Must hold
- **Never a singleton / static `Instance`.** Several puzzles live in one level. If the code still uses
  `PuzzleManager.Instance`, say that its owner must convert it before a second puzzle is built on it
  (§12) — do not convert someone else's file.
- `puzzleId` is unique and never renamed after saves exist.
- Silent restore: a solved puzzle re-fires `OnPuzzleCompleted` on load without sound, animation or dialogue.
- **An item that is used up must be the last thing the puzzle needs.** Consume only on the final success.
- Timer puzzles use the shared `PuzzleTimer`; timeout goes through the one game-over path.
- "You need a key" and "you need to be holding the key" are two messages.
- Gameplay code subscribes to `GameEvents`, never raises core events.

## 4. Stop and report if
It needs `Time.timeScale`, input locking, `SaveManager`, a new event, pathfinding or monster AI.

Tests: puzzle state and gating are EditMode tests (`write-tests`). Hand back with CLAUDE.md §10, in Thai.
