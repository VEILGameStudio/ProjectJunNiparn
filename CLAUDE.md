# Project Spec — 3D Puzzle / Point-and-Click Game (HD-2D)

This file is the single source of truth for this project. Read it before writing any code.

---

## 0. Critical rules

Everything in this file matters, but these seven are the ones that are expensive to reverse and easy
to break without noticing. Each is marked **CRITICAL** again where it is explained in full.

1. **Every subscriber subscribes in `OnEnable` and unsubscribes in `OnDisable`.** (§3)
   A missed unsubscribe breaks on the next scene load, and a beginner cannot trace it.
2. **Player input is camera-relative, never world-axis.** (§7)
   Get it wrong and pressing one key walks the character diagonally. This is the signature bug of
   this genre and it looks like a dozen other problems.
3. **Sprite materials use alpha clipping, never alpha blending.** (§7)
   Alpha-blended sprites flicker and swap order wherever they overlap. Discovering this late means
   reworking every sprite material and possibly the art itself.
4. **Sprites never cast shadows.** (§7) A flat card casts a flat card-shaped shadow. Use a blob shadow
   in `ThreeQuarter` levels; a blob is invisible in `SideView` and needs a different solution.
5. **Exactly two camera presets, `ThreeQuarter` and `SideView`. Never a third.** (§7)
   Each level picks one. Every sprite is drawn for one preset and lives in that preset's art folder;
   a sprite used in the wrong preset looks wrong and nothing warns you. Adding a third preset means
   a third set of art for everything shared — treat it as a hard stop.
6. **Never write code that sorts sprites by position.** (§1, §8)
   The depth buffer does this. The old Y-sorting system was deleted on purpose; do not rebuild it.
7. **Never report a test result you did not observe.** (§9)
   If the tests cannot run, say so and give the command.

---

## 1. The game

Unity **6.5.10f1**, **URP (3D Forward renderer)**. Genre: **story-driven** puzzle, point-and-click.

The story is the spine of the game and puzzles gate progress through it: a level is passed by solving
its puzzle. That makes Dialogue and Cutscenes primary systems, not decoration — they carry the game.

**A real 3D world seen through a fixed camera, filled mostly with 2D art.**
References: Pumpkin Panic, Cult of the Lamb, Octopath Traveler. This is the style usually called HD-2D.

**Two kinds of level, roughly half the game each.** They differ only in the camera preset the level
picks; the code is the same for both. Today one scene uses one preset throughout; later a single scene
will hold several areas with a preset each — see §12, and do not write code that forbids it.

| | `ThreeQuarter` | `SideView` |
|---|---|---|
| Looks like | a diorama seen from above at an angle | a 2.5D side-scroller |
| Camera | Orthographic | Perspective |
| Pitch / Yaw | 30° / 45° | 10° / 0° |
| Job in the game | puzzle rooms — the main gameplay | connecting corridors, a little puzzling, keeping the player on a path |
| Walkable depth | the whole floor | shallow, about **3 units** |

`SideView` levels are deliberately shallow. The player can step behind a pillar but cannot wander; the
narrow depth is the point, not a limitation. A level that needs real exploration is a `ThreeQuarter` level.

What is 3D geometry:
- The floor, walls, stairs, and anything the player walks on or bumps into.
- Colliders, physics and lighting are all ordinary 3D.

What is a 2D sprite:
- Every character — player, NPCs, monsters — drawn by hand, standing upright in the world and
  turned to face the camera.
- Decorative props — grass, flowers, small rocks, bushes, hanging lanterns, signs.

A large prop the player must walk around (a big tree, a pillar, a building) may be either: a sprite
with a 3D collider at its base, or real geometry. Decide per prop, and say which when adding one.

The camera never rotates during play. Because of that, sprites never need to spin to follow it: their
rotation is set once, from the level's camera angle.

Shared rules:
- Free WASD movement on the ground plane, never grid or tile based.
- Input is **camera-relative**: W always walks up the screen, which is a diagonal in world space.
- Mouse click = interact / pick up, by raycasting from the camera through the mouse position.
- Walls and obstacles are ordinary 3D colliders the player physically cannot pass through.
- About 24 levels. Doors link levels. Inventory and game state persist across scenes.

**CRITICAL —** depth sorting, depth scaling and parallax are **not systems in this project**. The depth
buffer handles all of that, and in `SideView` the perspective camera shrinks distant sprites by itself.
Never write code that sorts sprites by position or scales them by distance. An earlier version of this project had a Y-sorting system; it was deleted deliberately when
the game moved to 3D. Do not rebuild it, and do not "restore" it if you find traces of it.

Elevation (steps, raised platforms, bridges) is allowed — 3D handles it — but use it deliberately,
because it affects camera framing and how readable the floor is.

---

## 2. Who maintains this code

Two junior interns own this code day to day, not an experienced developer.

- **Intern A** just learned C#. Can read and modify simple scripts but cannot build systems alone.
- **Intern B** does not code. Has Godot experience and sets everything up in the Unity Inspector.

Every script must be easy for a beginner to read, understand, and use without asking for help.

---

## 3. Architecture

Two patterns, used consistently everywhere.

### Singleton — for managers only

One shared generic base class `Singleton<T> : MonoBehaviour`, written once and reused.
It handles: the `Instance` property, destroying duplicates on scene reload, and an Inspector flag for `DontDestroyOnLoad`.

Managers that are singletons: `GameManager`, `SaveManager`, `AudioManager`, `UIManager`, `GameOverManager`.
Nothing else. Never make a player, item, door, puzzle or trap a singleton.

### Observer — for everything crossing system boundaries

A static `GameEvents` class holds all game-wide C# events in one file:
`OnInventoryChanged`, `OnHealthChanged`, `OnStaminaChanged`, `OnItemPickedUp`, `OnDialogueStarted`, `OnDialogueEnded`, `OnPuzzleCompleted`, `OnGameOver`, `OnSceneReady`, plus `OnLanguageChanged` (kept because the UI still uses it to refresh text).

Systems raise events. UI and other systems subscribe. UI never reads manager internals directly.

**CRITICAL — every subscriber subscribes in `OnEnable` and unsubscribes in `OnDisable`. Every time. No exceptions.**
This is the single most damaging bug in this project: an event that is never unsubscribed breaks on scene reload and is very hard for a beginner to diagnose.

---

## 4. Code rules

1. Every script starts with a header comment: what it does, which GameObject it goes on, and what to assign in the Inspector.
2. Every important method gets ONE short summary comment above it. Do not comment every line. Add an inline comment only when a line is not obvious.
3. Design patterns are welcome when they make the code simpler (Singleton, Observer, State Machine, Object Pool). Do not explain the pattern in comments; just use it cleanly. Avoid syntax that hurts readability: long LINQ chains, nested lambdas, ternary chains, reflection, async/await.
4. Clear names, full words (`currentStamina`, not `curStam`). One class does one job. Keep scripts short.
5. Inspector-first: `[SerializeField] private` fields with `[Header]` groups and `[Tooltip]` on every field, so Intern B can configure everything without opening code.
6. Fail loudly and helpfully. A missing reference logs something like: `PlayerHealth on 'Player': Health Bar is not assigned. Drag the HealthBar UI here.`
7. Use `[RequireComponent]` where a script depends on another component.
8. Data lives in ScriptableObjects (items, dialogue, settings defaults). Never hard-code content in scripts.
9. New Input System only, keyboard + gamepad. No legacy `Input.GetKey`.
10. TextMeshPro for all text, with multi-language support including a Thai font fallback.
11. Comments in simple English.
12. **Do not create asmdef files.** Everything stays in the default `Assembly-CSharp`. This rule covers only our own code under `Assets/_Project`; asmdef files that ship with third-party packages (such as DOTween) are fine.

Expected comment style:

```csharp
// PlayerStamina
// Handles running stamina: drains while running, regenerates while walking or idle.
// Put this on: the Player GameObject.
// Assign in Inspector: Stamina Bar (UI), Tired Sound (optional).
public class PlayerStamina : MonoBehaviour
{
    // Reduces stamina while running. Forces walking when stamina reaches 0.
    private void DrainStamina() { ... }
}
```

---

## 5. Folder structure and ownership

```
Assets/_Project/
  Scripts/
    Core/            Singleton<T>, GameEvents, GameManager, SceneLoader,    [Oak]
                     LevelSettings, GameLayers
    Player/          Movement, Health, Stamina                              [Oak]
    Interaction/     IInteractable, InteractableBase, click & touch system  [Oak]
    Items/           ItemData, Inventory                                    [Oak]
    Dialogue/                                                               [Oak]
    Save/                                                                   [Oak]
    Settings/                                                               [Oak]
    Camera/          Cinemachine setup, SpriteBillboard                     [Oak]
    UI/                                                                     [Oak]
    Cutscene/                                                               [Oak]
    Gameplay/
      Puzzles/                                                              [Intern A]
      MiniGames/                                                            [Intern A]
      Traps/                                                                [Intern A]
      Interactables/                                                        [Intern A]
  Data/{Items,Dialogue,Settings}                                            [Intern B]
  Tests/           PlayMode tests
  Tests/Editor/    EditMode tests
  Prefabs, Scenes, Audio                                                    [Intern B]
  Art/
    Sprites/
      ThreeQuarter/  drawn for pitch 30 — only for ThreeQuarter levels      [Intern B]
      SideView/      drawn for pitch 10 — only for SideView levels          [Intern B]
      Shared/        angle-independent only: UI, icons, particles, VFX      [Intern B]
    Materials, Models, ...                                                  [Intern B]
```

**Never put a sprite from one preset folder into a level of the other preset.** It will look subtly
wrong — lit from the wrong height, floor contact at the wrong angle — and nothing in the editor warns
you. `Shared/` is only for art with no viewing angle at all. When in doubt it is not shared.

Gameplay code may **read** core systems but never edit them. If a gameplay task needs a change in a core folder, stop and say what is needed instead of editing it.

---

## 6. Interaction architecture

Two things are kept separate: **how something is activated** and **what it does**.

**Layer 1 — the contract (core, do not change):**

```csharp
public enum ActivationMode { Click, Touch, Both }

public interface IInteractable
{
    ActivationMode Activation { get; }
    bool CanInteract(PlayerContext player);
    void Interact(PlayerContext player);
}
```

`PlayerContext` is a small class holding what an interactable may need: transform, inventory, health.

**Layer 2 — `InteractableBase : MonoBehaviour, IInteractable` (core).**
Everything shared lives here so subclasses stay tiny:
activation mode, max interact distance, `oneShot`, `requiredItem`, `blockedMessage`, optional interact sound; distance and requirement checks; showing the blocked message; disabling itself after a one-shot; registering with the highlight system.
Exposes `protected abstract void OnInteract(PlayerContext player);`

**Layer 3 — concrete classes.** `PickupInteractable`, `DoorInteractable`, `MessageInteractable`, `PuzzleInteractable`, `QTEInteractable`. Interns add new ones here.

**Detection.** Click casts a ray from the camera through the mouse position with
`Physics.Raycast`, filtered by a LayerMask so it only sees the `Interactable` layer. The 3D depth test
already picks the nearest hit, so no manual sorting is needed. Touch is detected on the player and
never uses the click collider: `OnControllerColliderHit` when the player bumps an object's solid
`Blocking` collider, or `OnTriggerEnter` when the player walks into a trigger on the `TouchZone`
layer. A `CharacterController` does not raise `OnCollisionEnter` — `OnControllerColliderHit` is the
callback it does raise, so do not reach for the Rigidbody one. The cooldown restarts while the player
is still pushing against the same object, so holding into it counts as one touch and the player has
to step away before it fires again. Both paths call the same `Interact`. Never duplicate that logic.

**Colliders per interactable object.** A layer belongs to a whole GameObject, so each collider sits on
its own GameObject: the clickable one on the root, the others on children.

| Collider | Shape | Layer | Is Trigger | Job |
|---|---|---|---|---|
| Blocking | small, at the base | `Blocking` | no | stops the player walking through; the touch point for solid objects |
| Clickable | a box around the visible sprite or mesh | `Interactable` | yes | receives the mouse ray, never a touch |
| Touch zone (optional) | the area to walk into | `TouchZone` | yes | touch for walk-in spots that must not block: floor plates, cutscene spots, walk-through doorways, pickups on the floor |

Explain it to the level designer as: *the small one is what the character bumps into, the big one is
what the mouse clicks.* An object that is only decoration needs neither; an object that only blocks
needs the small one; an object that is only clicked needs the big one.

For a character or prop drawn as an upright sprite, the clickable box stands upright too and is about
the size of the drawn figure. It does not need to match the sprite exactly.

**Traps are not interactables.** A trap that damages on touch is `HazardTrap : MonoBehaviour`, dealing damage through `IDamageable` on a trigger. A trap that can be disarmed gets `HazardTrap` **plus** a `DisarmInteractable` on the same GameObject. Composition, not a second interface.

---

## 7. Systems

**Movement.** WASD on the ground plane, gamepad supported, free movement. Uses a
`CharacterController` — simple and predictable, no physics tuning, and it slides along walls for free.
Do not use a Rigidbody for the player unless something later genuinely needs to push it.

**CRITICAL — input is camera-relative.** The camera looks down at a fixed angle, so pressing W must move the
player *up the screen*, not along world +Z. Take the camera's forward and right, flatten them onto
the ground plane, normalize, and build the movement direction from those. Getting this wrong is the
most common bug in this kind of game: the character appears to walk diagonally when you press one key.

**Sprites in the world.** Characters and sprite props stand upright on the ground and face the camera.
`SpriteBillboard` sets the rotation from the camera's fixed angle:

- Rotation is set once in `Start`, not every frame, because the camera never rotates during play.
  This stays true under the `SideView` perspective camera: a sprite away from screen centre is then
  seen slightly off-axis and looks a little narrower, which is normal for this style and much better
  than having sprites swivel as the player walks past them. Never make billboards track the camera's
  *position*.
- Default is **yaw only** — the sprite stays vertical in the world, so it meets the floor naturally
  and reads correctly against 3D geometry.
- An Inspector field `tiltTowardCamera` (0 to the camera's pitch, default 0) leans the sprite back
  toward the camera. Higher values fight foreshortening but make the sprite look like it is lying
  down. This is a tuning knob for the art team, not something to change per object.
- If the camera angle is ever changed at runtime, call `Refresh()` on the billboards.

**CRITICAL — sprite materials must use alpha clipping, never alpha blending.** Alpha-blended sprites do not write
to the depth buffer, so they sort by object distance and flicker or pop when they overlap — which they
will, constantly. Use a URP Lit or Unlit material with Surface Type Opaque and Alpha Clipping on.
The cost is harder sprite edges; the art should be drawn with that in mind.

**CRITICAL — shadows.** The floor, walls and 3D props cast and receive real shadows. Sprites **do not cast**
shadows (Cast Shadows = Off on the renderer), because a flat card casts a flat card-shaped shadow.
A character or prop that needs to feel planted gets a separate soft blob shadow quad at its base
instead.

A blob shadow only works in `ThreeQuarter`. At the `SideView` pitch of 10° a flat quad on the floor is
seen nearly edge-on and compresses to about 17% of its depth — a thin sliver nobody will see. `SideView`
art grounds its figures another way: a contact shadow drawn into the sprite itself, or a shadow quad
standing at an angle rather than lying flat. Decide this with the art team before the first sprite is
drawn, not afterwards.

**Camera.** One Cinemachine camera per scene, following the player on X and Z, never rotating during
play. Its projection, angle and framing come from the level's preset:

| | `ThreeQuarter` | `SideView` |
|---|---|---|
| Projection | Orthographic | Perspective |
| Pitch / Yaw | 30 / 45 | 10 / 0 |
| Size / FOV | Orthographic Size 5 | Field of View 25 *(provisional)* |
| Camera distance | 20 | 20 *(provisional)* |
| Damping X / Z | 1 / 1.3 | 1 / 1.3 |

The `SideView` FOV and distance are **not final**. A wider FOV makes the character grow and shrink more
as it walks into depth and squashes sprites harder at the screen edges; a narrower one flattens the
scene toward the reference look. Both numbers must be locked before the art team draws its first
`SideView` sprite, and this table updated when they are.

**Level settings.** Each scene has one `LevelSettings` component. It holds the camera preset and the
per-level camera bounds, and applies them on scene start, so the level designer sets one dropdown
instead of remembering numbers. Every value stays overridable in the Inspector, including a Field of
View override used only by `SideView` levels.

**The code that applies a preset to a camera is a `public static` method, not a private method of
`LevelSettings`.** It takes a `CinemachineCamera` and a preset and sets the lens mode, size or FOV,
rotation and distance. `LevelSettings` only calls it. This is what lets §12's per-area camera zones be
added later without touching a core file.

**CRITICAL — two presets, never a third.** `ThreeQuarter` and `SideView` are decided and fixed, and each
level picks one. Every sprite is drawn for one of them. A third preset would mean a third version of
every sprite that appears in more than one kind of level, so adding one is a hard stop — ask first.
Within a preset, levels get their variety from layout, lighting and framing, never from nudging the
camera angle. Changing either preset's pitch after art production starts is a project-wide redraw.

**Items and inventory.** `ItemData` (ScriptableObject): id, localized name, localized description, icon, maxStack. Inventory supports add/remove/count and stacking, and raises `OnInventoryChanged`. The inventory UI subscribes and refreshes itself; it never touches inventory data directly.

**Dialogue.** Three types. *Full*: bottom box, speaker name, click to advance, blocks movement. *Choice*: same box plus Yes/No buttons with different outcomes via UnityEvent, blocks movement. *Mini*: a small bubble on a World Space canvas above the object or the player's head, fades out by itself, does not block movement. Content is authored as JSON by a writer and imported into ScriptableObjects. `OnDialogueStarted` / `OnDialogueEnded` lock and release player input.

**Stamina.** 0–100. Drains only while running. No regeneration while running; regenerates while walking or idle. At 0 the player is forced to walk and cannot run until stamina reaches **30**. Feedback when depleted: bar flash or shake plus a sound. Raises `OnStaminaChanged`.

**Health and damage.** `IDamageable` with `TakeDamage(int amount)`. Traps and monsters call it through triggers. Raises `OnHealthChanged`; at 0 raises `OnGameOver`.

**Timer.** A countdown used by some puzzles only, not every level. On timeout it plays a Fail animation (Animator trigger name set in the Inspector) and raises `OnGameOver`. It stops while the game is paused.

**Freeze time.** Opening the menu or inventory sets `Time.timeScale = 0`. UI still works while paused, using unscaled time.

**Save and load.** Two JSON files in `Application.persistentDataPath`. *Gameplay save*: inventory, current scene, player position, timer value, completed puzzle ids. *Global save*: settings. Autosave on scene load, item pickup and level complete. Continue loads on game start.

**Settings.** Music and Sound sliders through an AudioMixer. Brightness through a URP Global Volume, Color Adjustments → Post Exposure: slider minimum **10** is the normal default (Post Exposure 0), and the slider maximum is an Inspector field clamped between **50 and 70**. Language dropdown. All stored in the global save.

**Highlight.** A soft white outline on interactable objects via URP Shader Graph, following the object's silhouette. Shown when the player is near, and also when the player has not interacted with anything for a configurable idle time (hint system).

**Cutscenes.** Either a Timeline (`PlayableDirector`) or a video clip (`VideoPlayer`), with a Skip button. Used for: the storybook intro, the start of the game, and before collecting the puzzle gem. Player input is locked while one plays.

**Scene flow and game over.** Doors carry a target scene and a spawn point ID and go through `SceneLoader`. `GameManager` persists inventory and state across scenes. Game over: optional Fail animation → fade to black → wait a delay (Inspector field, 2–5 seconds) → load the last save. One implementation used by both death and timeout.

---

## 8. Extension points for the interns

These exist so interns can add content without touching core code.

- `InteractableBase` — subclass it, override `OnInteract`. That is the whole job.
- `PuzzleBase` — `puzzleId`, `oneTimeOnly`, `StartPuzzle()`, `CompletePuzzle()`, `FailPuzzle()`, plus UnityEvents `OnPuzzleStarted` / `OnPuzzleCompleted` / `OnPuzzleFailed` so Intern B can wire doors, sounds and animations in the Inspector with no code. Completion is recorded by the save system through `OnPuzzleCompleted(puzzleId)`.
- `MiniGameBase` — shows its UI, locks player input while it runs, returns success or failure, closes itself.
- `QuickTimeEvent : MiniGameBase` — Inspector-configurable: ordered input actions, time limit per input, allowed mistakes, prompt prefab. A new QTE should need no new code.
- `LevelSettings` — one per scene. Holds the camera preset and bounds and configures the level on start.
- `SpriteBillboard` — drop it on any sprite that should face the camera. No configuration needed in the normal case.

Gameplay code **subscribes** to `GameEvents` but never raises core events.

**Hard stops for gameplay work.** If a task requires any of these, stop and report it instead of writing code:
writing `Time.timeScale`; calling `SaveManager` or needing state to survive a scene reload; raising a core event; locking or unlocking player input; adding a field or changing a signature in a core class; adding a new event to `GameEvents`; pathfinding or monster AI; creating an asmdef; changing a camera preset's values or adding a third preset; building anything described in §12; writing any code that sorts sprites by position or scales them by distance.

---

## 9. Testing

Required for every system.

- **EditMode unit tests** for pure C# logic, where most tests belong: inventory add/remove/stack limits, stamina math and the 30 threshold, save data round-trip, localization lookup, timer countdown, puzzle state, camera-relative direction maths, and `GameEvents` subscribe/unsubscribe.
- **PlayMode integration tests** only for behaviour that needs the engine: click vs touch interaction, inventory surviving a scene load, game over reloading the save, input locked during dialogue.
- Do **not** test Unity itself. Never assert that `transform.position` changed after setting it.
- **Camera-relative movement must be tested at yaw 45, not only at yaw 0.** At the `SideView` yaw of 0
  the camera axes and the world axes coincide, so that case passes even if the code ignores the camera
  completely. Only the `ThreeQuarter` yaw of 45 can fail. Keep both cases; the yaw-45 one is the test.
- Do not write a test you could not make fail by breaking the production code.
- Every fixed bug gets a regression test that would have caught it.
- Tests live in `Assets/_Project/Tests/` (PlayMode) and `Assets/_Project/Tests/Editor/` (EditMode). Test Runner's "Enable playmode tests for all assemblies" must be turned on, because game code has no asmdef. Do not add an asmdef for game code to make tests compile — ask first.
- **That setting is currently OFF** (`playModeTestRunnerEnabled: 0` in `ProjectSettings/ProjectSettings.asset`). While it is off, do not write test files into the project. Put the test code in the report instead, and remind Oak to turn the setting on first. Delete this bullet once the setting is enabled.
- Test names state the behaviour: `Stamina_CannotRun_UntilRegeneratedTo30`. Arrange / Act / Assert with blank lines between.

**CRITICAL — after finishing a system, run the tests and report real results.** If you cannot run them, say so and give the exact command. Never report a result you did not observe. A guessed "all green" is worse than no test run at all.

---

## 10. What to hand back after each piece of work

1. Files created or changed, with full paths.
2. Inspector setup steps, written for someone who does not code.
3. Test results: passed / failed counts, and what each failure means in plain language.
4. A short manual checklist for anything tests cannot cover.

---

## 11. Third-party packages

The project uses exactly these two. Do not add other packages without asking.

### Cinemachine (version: 3.1.7)

Unity 6.x ships Cinemachine 3.x, whose API differs from the 2.x examples most code samples use. Before writing any Cinemachine code, check the installed version and use the matching API. If you are unsure which API applies, say so instead of guessing.

Cinemachine 3.x naming, for reference:

- namespace is `Unity.Cinemachine`, not `Cinemachine`
- `CinemachineCamera` (was `CinemachineVirtualCamera`)
- `CinemachinePositionComposer` (was `CinemachineFramingTransposer`)

Rules:

- One `CinemachineBrain` on the Main Camera. One `CinemachineCamera` follows the player.
- The projection depends on the level's preset: **Orthographic** for `ThreeQuarter`, **Perspective** for
  `SideView`. The camera is at a fixed angle and never rotates during play.
- **Switch projection through `CinemachineCamera.Lens.ModeOverride`, never by writing
  `Camera.main.orthographic`.** The Brain writes its lens onto the Main Camera every frame, so a value
  set directly on the camera is overwritten or not depending on script execution order — a bug a
  beginner cannot trace. `LensSettings.ModeOverride` is the supported switch and it is deterministic.
  Set `OrthographicSize` for `ThreeQuarter` and `FieldOfView` for `SideView`; they are separate fields.
- **`ModeOverride` does nothing unless `Lens Mode Override` is ticked on the `CinemachineBrain`.** The
  Brain only pushes the mode to the Unity camera when `CinemachineBrain.LensModeOverride.Enabled` is
  true, and it is **off by default**. Tick it in the Inspector on the Main Camera of every scene whose
  preset is not already the camera's projection; code reads this flag but never writes it. A scene that
  needs it and does not have it logs an error naming the exact Inspector step — the symptom otherwise
  is correct code with a camera that silently refuses to change projection.
- **A level can override framing, never the angle.** Zoom, field of view, distance, damping and bounds
  are per-level Inspector values. Pitch and yaw are not, and there is no Inspector field for them: they
  belong to the preset, and a per-level angle would be the third preset that §0 rule 5 forbids —
  created by an intern with one checkbox, in a level where every sprite was drawn for a different one.
- **`LevelSettings` resolves preset plus overrides in exactly one place, and everything reads it from
  there.** `Apply`, `CameraPitch` and `CameraYaw` all take their values from that one method; nothing
  calls the raw preset lookup directly. `SpriteBillboard` aims from `CameraPitch` and `CameraYaw`, so
  the moment those can disagree with what `Apply` gave the camera, every sprite in the level aims at an
  angle the camera is not at — and it looks like bad art, not like a bug, so nobody reports it.
  The test is end-to-end: after `Apply`, the camera's actual rotation must equal `CameraPitch` and
  `CameraYaw`.
- Camera bounds per level use `CinemachineConfiner3D` with a `BoxCollider` marked Is Trigger on the
  `Ignore Raycast` layer. The box limits where the **camera** may sit, not where the player may walk,
  so it is offset from the floor by the camera's own height and distance and is smaller than the floor
  by roughly half the visible width and depth on each side.
- **Under a perspective camera the visible width grows with distance, so size the back wall for the
  far plane, not the player plane.** Work out the half-width at the back wall's depth, add the camera's
  maximum sideways travel, and make the wall at least that wide. A back wall sized for the player plane
  leaves a visible gap at the ends of the level once the camera pans to either extreme.
- Never set the Main Camera's transform from a script. Cinemachine owns it. Anything that needs to move or shake the camera does it through Cinemachine (an Impulse Source, or by switching cameras), never by writing to `Camera.main.transform`.
- Camera settings live in the Inspector so the level designer can tune them without code.

### DOTween Pro

Used for tweening UI, fades, and small gameplay motion. Rules, in priority order:

1. **Pause safety.** This project sets `Time.timeScale = 0` when the menu or inventory is open.
   - A tween that must keep running while paused (menu UI, pause fades, settings panels) must use `.SetUpdate(true)` for unscaled time.
   - A gameplay tween (a door opening, an item bobbing, a trap moving) must not use it, so it freezes with the game.
   - State which one you chose whenever you write a tween.
2. **Lifetime safety.** A tween outliving its GameObject throws on scene change.
   - Every tween is linked to its object with `.SetLink(gameObject)`.
   - Any tween stored in a field is killed in `OnDisable`.
   - Never start a tween in `Update`.
3. Keep Safe Mode ON in the DOTween Utility Panel. This team is two beginners; the small performance cost is worth the protection.
4. Prefer the `DOTweenAnimation` component over code for anything the non-coding intern should be able to author or tweak — button feedback, panel slide-ins, highlight pulses. Only write tween code when it has to react to game state.
5. Keep tween durations and eases as `[SerializeField]` fields with `[Tooltip]`, never hard-coded, so timing can be tuned in the Inspector.
6. DOTween ships its own asmdef files. That is fine and expected — the "no asmdef" rule applies to our code under `Assets/_Project`, not to third-party packages.
7. After importing or updating DOTween Pro, run **Tools > Demigiant > DOTween Utility Panel > Setup**, otherwise the modules are not enabled.
8. DOTween modules needed: Audio, **Physics (3D)**, Sprites, UI, TextMesh Pro.
   - **Physics2D is now off and Physics (3D) is on** — this project moved from 2D to 3D.
   - UI Toolkit and all External Asset modules stay OFF.
   - If a tween API is missing, the module is off by design — ask before enabling one.

**Do not use `DOText()` for dialogue.** Thai combining characters break when revealed one index at a time. Use `TMP_Text.maxVisibleCharacters` advanced by grapheme cluster instead.

---

## 12. Planned, not built yet

This section describes where the project is going. **Do not build any of it now.** Its purpose is to
stop today's code from closing a door that is going to be needed. If a task seems to require something
here, stop and ask.

### Camera zones inside one scene

A single scene will eventually hold several areas, each with its own camera preset: the player walks
through a door into a `ThreeQuarter` puzzle room, solves it, and leaves through another door into a
`SideView` corridor — all without a scene load. A `CameraZone` component will own a trigger volume, a
`CinemachineCamera` and a preset, and raise its camera's priority when the player enters.

Three constraints follow from this, and the first two are level-design rules, not code:

1. **Two zones of different presets must never be visible from each other.** A sprite drawn for a 30°
   pitch looks wrong the moment it appears in a 10° camera's frame. Zones connect through doors, turns
   or anything that blocks the sightline — never across an open space. This is the expensive one: it
   constrains level layout, and it cannot be fixed after the art is drawn.
2. **The transition is a cut behind a fade, never a blend.** Cinemachine cannot blend between an
   orthographic and a perspective camera — `OrthographicSize` and `FieldOfView` are different units, so
   an interpolated value is meaningless and the picture jumps mid-blend. Set a Cut in the Brain's
   Custom Blends asset for these pairs and hide it behind the same fade the door transitions use.
3. **Billboards must re-aim on a zone change.** `SpriteBillboard` sets its rotation once in `Start`,
   which is correct for one preset per scene. With zones, each billboard registers itself in a static
   list in `OnEnable` and removes itself in `OnDisable` — the same rule as every other subscriber in
   §3 — and a single `RefreshAll()` re-aims the active ones on a zone change.

### Level structure

A level is one scene containing several sub-areas (1.1, 1.2, 1.3 …). Clearing all of them loads the
next level's scene. Doors inside a scene move the player between sub-areas; doors between levels go
through `SceneLoader` as they do today.

**Each sub-area is one parent GameObject, deactivated until the player enters it.** A scene holding
five sub-areas otherwise loads all five at once, which is more in memory than five separate scenes, not
less. Deactivating also keeps other zones out of frame on its own, and keeps `RefreshAll()` cheap.

This works because of §3: deactivating an area disables its scripts, which unsubscribe in `OnDisable`,
and re-enabling resubscribes. The one rule it adds — **state belonging to a sub-area lives in a manager,
never on a GameObject that gets deactivated**, because `OnEnable` runs again and local state is lost.