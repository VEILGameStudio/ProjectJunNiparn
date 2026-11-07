# UGUI performance and focus (extra notes for add-ui-screen)

Adapted from the Unity UI specialist in claude-code-game-studios (see `NOTICE.md`). The UI Toolkit half
of that source was dropped: this project uses **UGUI + TextMeshPro** only. Read this when a screen is
slow, has lists, or when gamepad focus gets lost.

## Canvases
- **Changing one element rebuilds its whole Canvas.** Put often-changing UI (stamina bar, timers,
  floating hints) under its own child Canvas, apart from static frames and labels. Do not add one Canvas
  per element either: one per group that changes together.
- Set `sortingOrder` explicitly when Canvases overlap; do not rely on hierarchy order.
- Hide or fade a group with a **`CanvasGroup`** (alpha, interactable, blocksRaycasts), not by setting each
  Image's alpha. Disabling a GameObject is fine for a screen that is closed for a long time.
- Turn **Raycast Target off** on every Image and TextMeshPro that is not clickable (backgrounds, icons,
  labels). It saves raycasts and stops invisible graphics stealing clicks.

## Layout
- Avoid Layout Groups nested inside Layout Groups; they recalculate together. Use anchors for fixed
  positions and keep Layout Groups for lists.
- A long list (inventory grid, gallery) re-uses a pool of slot objects instead of creating and destroying
  them each time it opens.
- Cache `RectTransform` and component references in `Awake`; do not look them up when redrawing.
- UI sprites share a Sprite Atlas so a panel draws in few batches (check in the Frame Debugger).

## Focus (gamepad)
- When a screen opens, select its most useful element (`EventSystem.current.SetSelectedGameObject`).
- When it closes, **give focus back** to the button that opened it. Store it when opening.
- A modal popup **traps focus**: its buttons' Navigation must not lead to buttons behind it.
- Set Navigation explicitly where Automatic jumps to the wrong button (Visualize in the Inspector).

## UI and game state
- UI never changes game data directly. A click asks the system (`Hold`, `SetMusicVolume`), the system
  raises a `GameEvents` event, and the UI redraws from it (CLAUDE.md §3).
- Do not mix in UI Toolkit (`UIDocument`) on a screen — it is not used in this project.

## Measuring
- Profiler → UI and UI Details modules show Canvas rebuilds and batch counts. Confirm a problem there
  before restructuring a screen, and say what you measured.
