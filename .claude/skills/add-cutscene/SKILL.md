---
name: add-cutscene
description: Use when a teammate wants a cutscene, story scene, intro, ending, video clip or Timeline sequence - "play a scene when the player enters", "show a video before the gem", a Skip button, or something that happens after a cutscene ends.
---

# Add a cutscene

Spec: CLAUDE.md §7 "Cutscenes", "Camera", "Dialogue"; §8 hard stops. The story is the spine of this game,
so cutscenes carry it — but the system that plays them already exists. **A new cutscene is content and
Inspector setup, not new code.**

## 1. What already exists (re-list the folder first; this goes stale)
- `Scripts/Cutscene/CutsceneManager.cs` [Oak] — on the `PersistentCanvas`, reached through
  `UIManager.Instance.Cutscene`. Plays a Timeline (`PlayableDirector`) or a video (`VideoPlayer`), shows the
  Skip button, sets `GameState.Cutscene`, and switches Gameplay input off and back on.
- `Scripts/Cutscene/CutsceneTrigger.cs` [Oak] — an `InteractableBase`. **Play On**:
  - `Scene Start` — the storybook intro, plays when the scene loads
  - `Interact` — click it, or walk in with Activation = Touch (before collecting the puzzle gem)
  - `Manual` — a button's OnClick calls `Play()` (start game)
  - **On Cutscene End** (UnityEvent) runs after the end *or* a skip — give the gem, open the door here.
  - **One Shot** + a Unique Id: plays only the first time and is saved as `<scene>/<id>`. Interact mode is
    saved when it starts; Scene Start and Manual when it ends or is skipped (quit half-way through an intro
    and it shows again next time).

Gameplay code never locks input or sets the game state itself — `CutsceneManager` does both (§8 hard stop).

## 2. Climb the ladder
| Need | How | Who |
|---|---|---|
| a cutscene in a scene | `CutsceneTrigger` + a Timeline or video, all in the Inspector | Intern B |
| something happens after it | wire **On Cutscene End** | Intern B |
| it should play once | tick **One Shot**, give a Unique Id | Intern B |
| it needs a choice or a branch inside it | Choice dialogue (`add-dialogue`), with the outcomes on the Choice's events | Intern B |
| genuinely new behaviour no field or event can express | stop → Oak (the cutscene folder is core) | — |

## 3. Timeline or video?
| | Timeline | Video |
|---|---|---|
| Use for | scenes played with the game's own characters and sets | painted storybook pages, pre-rendered movies |
| Pause (menu / inventory) | stops by itself: `PlayableDirector` in **Game Time** follows `timeScale` | `CutsceneManager` pauses it and resumes it (a `VideoPlayer` ignores `timeScale`) |
| Language | dialogue inside it is a dialogue asset, so it is translated | text burned into a video is not; one clip per language, or no words in the video |
| Size | small | large; keep clips short |

Timeline settings: Update Method **Game Time**, Play On Awake **off** (the trigger plays it), Wrap Mode **None**
(a held or looped director never raises `stopped`, so the cutscene never ends).

## 4. Rules that are easy to break
- **Camera.** The area camera's values are locked and never adjusted by hand (§7, §11). A cutscene does not
  get its own angle: sprites are drawn and stretched for the preset's pitch and yaw, and a camera at any
  other angle makes every character look wrong. Animating a Cinemachine camera's position, rotation or
  lens inside a Timeline is the same thing as editing it by hand. Close-ups, dollies and letterbox are a
  pending proposal (`Docs/proposals/2026-11-03-cutscene-dolly-camera.md`) — until Oak decides, stop and
  point to it. In `ThreeQuarter` rooms a cutscene camera would also need to stay orthographic.
- **Animate characters, not cameras.** Move NPCs with an Animation track or Activation tracks; let the
  area camera keep following.
- **Text in a cutscene** is a dialogue asset (Full for a conversation, Mini for a reaction), never a
  typed string or text baked into a sprite.
- **Sound in a cutscene** goes on a Timeline Audio track, routed to the mixer's `SFX` or `Music` group
  (`add-audio`), never `AudioSource.Play` dragged into On Cutscene End — that event also fires on skip.
- **Skipping must leave the world in its finished state.** Whatever the cutscene does that matters (an NPC
  that walked to the door, a door that opened) must also be set in **On Cutscene End**, because Skip stops
  the Timeline half-way. Test the skip, not only the full play.
- **One cutscene at a time.** `CutsceneManager` ignores a second `Play` while one is playing; do not chain
  two triggers to fire in the same frame — start the second from the first one's On Cutscene End.
- **Restore.** A one-shot cutscene that already played does not play again after loading a save, and its
  On Cutscene End does not re-run — so anything it opened must be saved by what it opened (a one-shot door,
  a puzzle's restore), not only by the cutscene. Check this with a reload.

## 5. Inspector steps (write them for Intern B)
1. Make sure the scene has the `PersistentCanvas` prefab (it carries `UIManager` + `CutsceneManager`
   + the Skip button).
2. Create an empty object, add `PlayableDirector`, make a Timeline asset in the level's folder; Update Method
   Game Time, Play On Awake off, Wrap Mode None. (Video: add `VideoPlayer`, Play On Awake off, Loop off,
   Render Mode to the `PersistentCanvas` video screen.)
3. On the object that starts it, add `CutsceneTrigger`: Play On, Cutscene Type, drag the director or video.
4. Interact mode: the click box (`Interactable` layer, trigger) or a touch zone (`TouchZone` layer) — see
   `add-interactable` §3.
5. One Shot + a Unique Id that is unique in this scene.
6. Wire On Cutscene End.

## 6. Known gaps — report, do not patch in gameplay code
- The Skip button has no confirm and no "hold to skip". Changing that is a core UI change → Oak.

## 7. Hand back (Thai, CLAUDE.md §10) — manual checklist
- [ ] plays at the right moment; input is locked while it plays; movement returns after it
- [ ] Skip works, and after skipping the world is in the same state as after watching it fully
- [ ] open the menu mid-cutscene: Timeline or video stops, and carries on when the menu closes
- [ ] One Shot: reload the save — it does not play again, and what it opened is still open
- [ ] Thai and English: every line in it changes with the language setting
- [ ] the camera never changed angle or projection during it
