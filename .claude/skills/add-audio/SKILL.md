---
name: add-audio
description: Use when a teammate wants to add or change music, a sound effect, an ambience, a footstep, a UI click, a sound on an object or in a cutscene, or says a sound is too loud, too quiet, plays twice or keeps playing while paused.
---

# Add or change sound and music

Spec: CLAUDE.md §7 "Settings", "Save and load" (restore rule), "Cutscenes"; §3 Singleton.

## 1. What already exists (re-check before using)
- `Scripts/Core/AudioManager.cs` [Oak] — singleton on the Managers object.
  `AudioManager.Instance.PlayMusic(clip)` loops one music clip; `PlaySfx(clip)` plays a one-shot.
  `SetMusicVolume` / `SetSoundVolume` are called **only** by `SettingsMenu`.
- `Audio/GameMixer.mixer` — groups **Master → Music, SFX**; exposed parameters `MusicVolume`, `SoundVolume`
  (the Settings sliders). Every `AudioSource` in the game routes its **Output** to `Music` or `SFX`,
  or the Settings sliders do nothing to it.
- `InteractableBase` has an **Interact Sound** field: played on every use, never on restore.
- `PlayerStamina` has a **Tired Sound** field — the same pattern for other components.

## 2. Climb the ladder
| Need | How | Who |
|---|---|---|
| a sound when something is clicked / touched | the component's **Interact Sound** (or its own sound field) | Intern B |
| a sound inside a cutscene | an Audio track in the Timeline, output → `SFX` / `Music` group | Intern B |
| a looping ambience / a sound from a place in the world (waterfall, fire) | an `AudioSource` on that object: Loop, Play On Awake, Output `SFX`, Spatial Blend 1 if it should fade with distance | Intern B |
| music for a level | a call to `PlayMusic` from the level's start (ask how this level starts its music — there is no per-level music field yet) | Intern A / Oak |
| a sound on a new gameplay event | a sound field on that gameplay component + `PlaySfx` | Intern A |
| volume groups, ducking, a new mixer parameter, crossfades | core change → Oak | — |

## 3. Rules that are easy to break
- **Never drag a sound into a UnityEvent that restore re-fires** (`OnPuzzleCompleted`, `OnOpened` on a
  one-shot). Restore re-fires the event on every load, so the sound plays at scene start forever. Sounds go
  in the component's own sound field, which restore skips (§7 Save).
- **No new audio manager, no `static Instance` on a sound script** (§3). Use `AudioManager` or a plain
  `AudioSource`.
- **Pause.** `Time.timeScale = 0` does **not** stop an `AudioSource`. One-shots finish, loops keep going.
  For gameplay loops that must stop with the game (a trap's whirr, a timer tick) the owning script reads
  `GameManager.Instance.PauseManager.IsPaused` and calls `AudioSource.Pause()` / `UnPause()` when it changes
  (reading core is allowed; there is no pause event, so check it in `Update`). Never `AudioListener.pause`
  — it also silences the menu. UI clicks on a paused menu should keep playing (that is correct).
- **Volume from Settings only** (§7). Gameplay never writes the mixer parameters or `AudioListener.volume`.
  Make a clip quieter by its source's Volume or by re-exporting it, never by calling `SetSoundVolume`.
- **One `AudioListener`**, on the Main Camera. A second one logs a warning every frame.
- **New Input System** for any test key that plays a sound (§4.9).
- **Thai voice lines** (if they ever exist) belong to the dialogue asset, so they change with the language.

## 4. Import settings
| Kind | Load Type | Compression | Notes |
|---|---|---|---|
| short SFX (< 1 s) | Decompress On Load | ADPCM or Vorbis q≈70 | Force To Mono for 3D sounds |
| ambience / long SFX | Compressed In Memory | Vorbis | Loop points clean (no click) |
| music | **Streaming** | Vorbis q≈60–70 | never Decompress On Load (huge memory) |
Put files under `Assets/_Project/Audio/` (`Music/`, `SFX/`, `Ambience/`), named by what they are
(`sfx_chest_open`, `mus_forest_day`), not `New Audio 3`.

## 5. Stop and report if
It needs a new field or method on `AudioManager`, a new mixer group or exposed parameter, a new
`GameEvents` event (for example a pause event), or a new package (no third-party audio tools; §11).

## 6. Hand back (Thai, CLAUDE.md §10) — manual checklist
- [ ] the Music / Sound sliders change it (proves the Output group is right)
- [ ] plays once per action — not twice, not at scene start
- [ ] reload a save: no sound plays by itself on load
- [ ] open the menu: gameplay loops behave as intended; UI clicks still sound
- [ ] no "2 audio listeners" warning in the Console
