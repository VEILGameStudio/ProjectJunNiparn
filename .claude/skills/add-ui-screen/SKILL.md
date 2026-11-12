---
name: add-ui-screen
description: Use when a teammate wants a new or changed menu, panel, popup, HUD element, button, settings tab or any on-screen UI - main menu, pause popup, credits, an extras/gallery screen, a puzzle screen, or a UI animation.
---

# Add or change a UI screen

Spec: CLAUDE.md §1 "Aspect ratio", §3 Observer, §4.10, §7 "The inventory screen", "Settings", "Freeze time",
§11 DOTween. Most UI is Inspector work for Intern B; code is only for UI that reacts to game state.

## 1. What already exists (re-list `Scripts/UI/` and `Scripts/Settings/` first)
| Thing | Use it for |
|---|---|
| `PersistentCanvas` prefab (`UIManager`) | the canvas that survives scene loads: dialogue, cutscene, fader, inventory, HUD |
| `ScreenSwitcher` | one screen at a time (main menu ↔ settings ↔ credits, or tabs inside a panel); `Open` / `Back` / `CloseCurrent` / `QuitGame` from buttons; "open" / "close" `DOTweenAnimation` ids |
| `ScreenFader` | fade to / from black (unscaled time) — never write a second fader |
| `UIStatBar` | a bar driven by an event (health, stamina) |
| `InventoryUI` + `InventorySlotUI` + `HeldItemDisplay` | the bag and the hand |
| `SettingsMenu` + `BrightnessController` | music / sound / brightness / language |
| `PauseManager` (`GameManager.Instance.PauseManager.Pause(this)` / `Resume(this)`) | freezing the game while a screen is open; it counts sources |

A new screen is almost always a panel + `ScreenSwitcher` + buttons wired in the Inspector. New code only
when the screen must show live game state.

## 2. Canvas and size — fix before laying anything out
- Canvas Scaler: **Scale With Screen Size, 1920 × 1080, Match 0.5** (already on `PersistentCanvas`; a new
  canvas in a menu scene must copy it). Never Constant Pixel Size.
- **Every panel must fit in 935 canvas units of height** — that is the canvas height at 21:9. A panel built
  in a 16:9 Game view (1080 high) can be broken at 21:9 and look fine while you build it. Check in the
  Game view at 21:9 (2560 × 1080) and at 16:9 before handing back.
- Anchor to edges and corners, not the centre, for anything near a screen edge (the bag button sits bottom
  right). Use Layout Groups for lists; a list that can grow scrolls.
- One event system per scene with **`InputSystemUIInputModule`** (New Input System). Never the old
  `StandaloneInputModule` (§4.9).

## 3. Pause, time and tweens (§11 DOTween, CLAUDE.md "Freeze time")
- A screen that should stop the game calls `PauseManager.Pause(this)` when it opens and `Resume(this)` when
  it closes — from code, or through a small bridge component from On Opened / On Closed. Never write
  `Time.timeScale` (§8 hard stop).
- **Everything that animates on a paused screen runs on unscaled time**: `DOTweenAnimation` → tick
  **Ignore TimeScale**, untick Auto Play when `ScreenSwitcher` drives it; code tweens → `.SetUpdate(true)`;
  `Animator` → Update Mode **Unscaled Time**. `ScreenSwitcher` logs a warning when it finds one that is not.
- HUD and gameplay feedback that must freeze with the game do **not** use unscaled time.
- Every code tween: `.SetLink(gameObject)`; a stored tween is killed in `OnDisable`; never started in `Update`;
  duration and ease are `[SerializeField]` fields with tooltips. Say which time mode you chose.
- Prefer a `DOTweenAnimation` component over code for button feedback, slide-ins and pulses.

## 4. Data and events (§3)
- UI **subscribes** to `GameEvents` in `OnEnable` and unsubscribes in `OnDisable`, then redraws.
  It never reads a manager's internals and never edits game data — it asks (`Hold`, `StopHolding`,
  `SetMusicVolume`) and redraws from the event.
- Put the listening script on an object that **stays active**, not on the panel it shows and hides —
  a script on a hidden panel stops hearing events and keys (the `InventoryUI` note).
- A key that opens a screen is an action in the `GameControls` input asset read through `InputReader`,
  never `Input.GetKey`.

## 5. Text
- TextMeshPro only, with the project's font asset that has the **Thai fallback**. Never a legacy `Text`,
  never a new font asset without checking Thai renders (สระ and วรรณยุกต์ stacked correctly).
- Words the player reads come from a `LocalizedString` or a dialogue asset, and refresh on
  `GameEvents.OnLanguageChanged`. Do not type Thai or English straight into a label that the player reads
  in a level. There is no "localized label" component yet; if a screen needs many static translated labels,
  ask Oak before writing one (it would live in `Scripts/UI/`, a core folder).
- Thai is taller than English at the same size: leave room, and test both languages at 21:9.
- Never reveal text with `DOText()` (breaks Thai); use `maxVisibleCharacters` by grapheme cluster.

## 6. Gamepad
- Every screen is usable with a gamepad: set the first selected button when it opens, check Navigation
  (Visualize in the Inspector) so focus cannot get lost, and Back / Cancel closes it.

## 6b. Slow screens, lists, focus
Canvas rebuilds, Raycast Target, pooled lists, returning focus after a popup: `references/ugui-performance.md`.

## 7. Ownership and stops
- `Scripts/UI/`, `Scripts/Settings/` and the `PersistentCanvas` prefab are core [Oak]. Intern B builds
  panels and wiring in scenes and prefabs they own; adding a panel *inside* `PersistentCanvas` is a change to
  an Oak prefab → ask first.
- Stop and report if a screen needs: a new `GameEvents` event, `SaveManager` calls, brightness changed from
  outside the settings system, input locking, or `Time.timeScale`.

## 8. Hand back (Thai, CLAUDE.md §10) — manual checklist
- [ ] fits and is readable at 21:9 (935 high) and 16:9, in Thai and in English
- [ ] opens and closes with mouse, keyboard and gamepad; focus never lost
- [ ] if it pauses: gameplay frozen, its own animations still play; closing resumes (and closing one of two
      open screens keeps the game paused)
- [ ] changing language while it is open updates it
- [ ] load another scene with it open: no errors in the Console
