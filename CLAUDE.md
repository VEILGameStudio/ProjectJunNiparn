# Project Spec — 3D Puzzle / Point-and-Click Game (HD-2D)

This file is the single source of truth for this project. Read it before writing any code.

---

## 0. Critical rules

Everything in this file matters, but these eight are the ones that are expensive to reverse and easy
to break without noticing. Each is marked **CRITICAL** again where it is explained in full.

1. **Every subscriber subscribes in `OnEnable` and unsubscribes in `OnDisable`.** (§3)
   A missed unsubscribe breaks on the next scene load, and a beginner cannot trace it.
2. **Player input is camera-relative, never world-axis.** (§7)
   Get it wrong and pressing one key walks the character diagonally. This is the signature bug of
   this genre and it looks like a dozen other problems.
3. **Sprite materials use alpha clipping, never alpha blending.** (§7)
   Alpha-blended sprites flicker and swap order wherever they overlap. Discovering this late means
   reworking every sprite material and possibly the art itself.
4. **Sprites never cast shadows.** (§7) A flat card casts a flat card-shaped shadow. Use a blob shadow.
5. **Exactly two camera presets, `ThreeQuarter` and `SideView`. Never a third.** (§7)
   Each area picks one. Every sprite is drawn for one preset and lives in that preset's art folder;
   a sprite used in the wrong preset looks wrong and nothing warns you. Adding a third preset means
   a third set of art for everything shared — treat it as a hard stop.
6. **Character and prop sprites are stretched vertically when they are made, and the card is never
   tilted to compensate.** (§7) The two fixes cancel the same thing out, so using both makes every
   character 15.5% too tall with nothing on screen that reads as a bug.
7. **Never write code that sorts sprites by position.** (§1, §8)
   The depth buffer does this. The old Y-sorting system was deleted on purpose; do not rebuild it.
8. **Never report a test result you did not observe.** (§9)
   If the tests cannot run, say so and give the command.

---

## 1. The game

Unity **6.5.10f1**, **URP (3D Forward renderer)**. Genre: **story-driven** puzzle, point-and-click.

The story is the spine of the game and puzzles gate progress through it: a level is passed by solving
its puzzles. That makes Dialogue and Cutscenes primary systems, not decoration — they carry the game.

**A real 3D world seen through a fixed camera, filled mostly with 2D art.**
References: Pumpkin Panic, Cult of the Lamb, Octopath Traveler. This is the style usually called HD-2D.

**Two kinds of area, roughly half the game each.** They differ only in the camera preset the area
picks; the code is the same for both. A scene may be one area, or hold several camera zones with a
preset each — see §7.

| | `ThreeQuarter` | `SideView` |
|---|---|---|
| Looks like | a diorama seen from above at an angle | a 2.5D side-scroller |
| Camera | Orthographic | Perspective |
| Pitch / Yaw | 30° / 45° | 10° / 0° |
| Job in the game | puzzle rooms — the main gameplay | connecting corridors, a little puzzling, keeping the player on a path |
| Walkable depth | the whole floor | shallow, about **3 units** |
| Camera follows the player | loosely, so the whole room stays readable | closely left and right, not into depth |

`SideView` areas are deliberately shallow. The player can step behind a pillar but cannot wander; the
narrow depth is the point, not a limitation. An area that needs real exploration is `ThreeQuarter`.

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
rotation is set once from the area's camera angle, and re-aimed only when a camera zone changes.

Shared rules:
- Free WASD movement on the ground plane, never grid or tile based.
- Input is **camera-relative**: W always walks up the screen, which is a diagonal in world space.
- Mouse click = interact / pick up, by raycasting from the camera through the mouse position.
- Walls and obstacles are ordinary 3D colliders the player physically cannot pass through.
- About 24 levels. Doors link levels. Inventory and game state persist across scenes.

**CRITICAL —** depth sorting, depth scaling and parallax are **not systems in this project**. The depth
buffer handles all of that, and in `SideView` the perspective camera shrinks distant sprites by itself.
Never write code that sorts sprites by position or scales them by distance. An earlier version of this
project had a Y-sorting system; it was deleted deliberately when the game moved to 3D. Do not rebuild
it, and do not "restore" it if you find traces of it.

**Parallax backgrounds are built by placing layers, not by writing a script.** `SideView` is a
perspective camera, so a layer further from the camera already slides more slowly — that is what
perspective *is*. A layer at distance *D* moves at 20/*D* of the play plane's rate, so 40 gives half
speed, 100 gives a fifth, 200 gives a tenth. Pick the distance that gives the speed; there is nothing
to code. Two consequences the art has to know, at 1080p:

- The visible height at distance *D* is 0.335 × *D* world units, so **PPU = 1080 ÷ (0.335 × D)**.
  A backdrop 100 units out is drawn at PPU 32 against the foreground's 161. Export it at the character
  PPU and it comes out five times too small.
- The width has to cover the screen *plus* the camera's whole travel: 0.335 × *D* × aspect, which at
  21:9 and *D* = 100 is 79 units before travel is added. Far layers are large. The 36-unit back wall is
  a near-plane number and does not apply to them.

**`ThreeQuarter` cannot have parallax at all.** It is orthographic, and an orthographic projection has
no perspective divide, so distance has no effect on apparent motion — a layer 500 units back moves
exactly like one 5 units back. No placement fixes this and no script is allowed to fake it. Those
rooms are enclosed anyway; depth there comes from fog, from a darker and less saturated far layer, and
from SSAO. If a scene genuinely needs a moving horizon, it is a `SideView` scene.

Elevation (steps, raised platforms, bridges) is allowed — 3D handles it — but use it deliberately,
because it affects camera framing and how readable the floor is.

**Aspect ratio. The widest supported aspect is 21:9.** 16:9 is the baseline and 21:9 is the limit every
number has to survive; anything wider is allowed to look wrong. Vertical framing is fixed and never
changes with the window: a wider screen shows more to the left and right, never more above or below.
That falls out of a perspective camera's vertical FOV and an orthographic camera's size, so the camera
itself needs no code for it — but camera bounds and the width of back walls must hold at 21:9, not just
16:9. Back walls are 36 wide, which actually holds to 32:9. See §7 Level settings.

The same limit governs UI. The canvas scaler is Match 0.5, so canvas height shrinks as the window
widens — 1080 units at 16:9, 935 at 21:9 — and a panel taller than 935 units is broken at the widest
supported aspect even though it looks fine while you build it. Measure panels in canvas units against
935, not against the window you happen to be using.

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
Nothing else. Never make a player, item, door, puzzle or trap a singleton. A level holds several
puzzles at once, so a puzzle that is a singleton breaks the second one silently.

### Observer — for everything crossing system boundaries

A static `GameEvents` class holds all game-wide C# events in one file:
`OnInventoryChanged`, `OnHeldItemChanged`, `OnHealthChanged`, `OnStaminaChanged`, `OnItemPickedUp`, `OnDialogueStarted`, `OnDialogueEnded`, `OnPuzzleCompleted`, `OnGameOver`, `OnSceneReady`, `OnCameraZoneChanged`, plus `OnLanguageChanged` (kept because the UI still uses it to refresh text).

`OnHeldItemChanged` carries the held `ItemData`, or `null` for an empty hand. Holding the same
item again does not raise it.

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
9. New Input System only, keyboard + gamepad. **No legacy `Input.GetKey` in our own code.**
   `activeInputHandler` is deliberately left on **Both**, so a legacy call still compiles and runs —
   which means nothing stops it but a person reading the code. Treat it as a review item: any legacy
   input call in a pull request or a handback is sent back, and anything that genuinely needs the old
   API is named here as an exception rather than slipped in quietly.
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
                     LevelSettings, CameraPreset, GameLayers
    Player/          Movement, Animator, Health, Stamina                    [Oak]
    Interaction/     IInteractable, InteractableBase, click & touch system  [Oak]
    Items/           ItemData, Inventory                                    [Oak]
    Dialogue/                                                               [Oak]
    Save/                                                                   [Oak]
    Settings/                                                               [Oak]
    Camera/          Cinemachine setup, SpriteBillboard, CameraZone         [Oak]
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
      ThreeQuarter/  drawn for pitch 30 — only for ThreeQuarter areas       [Intern B]
      SideView/      drawn for pitch 10 — only for SideView areas           [Intern B]
      Shared/        angle-independent only: UI, icons, particles, VFX      [Intern B]
    Materials, Models, ...                                                  [Intern B]
```

**Never put a sprite from one preset folder into an area of the other preset.** It will look subtly
wrong — lit from the wrong height, meeting the floor at the wrong angle, and stretched by the wrong
amount — and nothing in the editor warns you. `Shared/` is only for art with no viewing angle at all.
When in doubt it is not shared.

Gameplay code may **read** core systems but never edit them. If a gameplay task needs a change in a core folder, stop and say what is needed instead of editing it.

**Reading and editing are different things.** Every file in the project may be read, called, inherited
from, attached to a GameObject, filled in from the Inspector, and subscribed to, whoever owns it. The
only thing an owner tag forbids is **editing the contents of the file**. Anyone who reads a folder tag
as "don't use this" and writes their own copy of something that already exists there has made the
problem worse than editing it would have.

**This map is not true on disk yet.** Several paths differ from it. The ones that have to move are
listed in §12, with the order to move them in; the rest simply stay where they are. Until then, these
paths are what is real:

| On disk | Owner | Rule |
|---|---|---|
| `Assets/Puzzle/` — `PuzzleManager.cs`, `123.unity`, `UI/`, `box/` | intern | Theirs to edit. Nobody moves, renames, or deletes-and-recreates these. |
| `Assets/_Project/Prefabs/Puzzles/ChestPuzzle.prefab` | intern | Same. |
| `Assets/Inventory/ItemData.cs` | Oak | Core, despite sitting outside `Scripts/`. Same rule as any Oak file. |

`Scripts/Interaction/Chest.cs` was written by an intern but is a **core interactable owned by Oak**,
under the same rule as everything else in that folder. `Scripts/Interaction/` has no exceptions in it.

`123.unity` and `Test_3D.unity` are never opened for editing by anyone but their owner. A scene is the
easiest file in Unity to break by accident and the hardest to review in a diff.

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

**Layer 3 — concrete classes.** `PickupInteractable`, `ContainerInteractable`, `DoorInteractable`, `ZoneDoor`, `MessageInteractable`, `PuzzleInteractable`, `QTEInteractable`. Interns add new ones here — but read §8 first, because most new ideas are a field on an existing class, not a new class.

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

**Facing.** `PlayerAnimator` on the `Visual` child drives a Unity `Animator`, one controller per preset,
swapped when the camera zone changes. It takes the facing from the move direction **after that
direction has been converted into screen space** — the same camera-relative conversion the movement
uses. `ThreeQuarter` snaps the screen-space direction to 8 compass directions; `SideView` snaps to left
or right only, mirrored with the Visual's Scale X. Snapping in world axes instead puts every character
45° out in `ThreeQuarter` areas, which looks like bad art rather than a bug. The snapping itself is a
`public static` method so it can be tested without a scene.

### Sprites in the world

Characters and sprite props stand upright on the ground and face the camera. `SpriteBillboard` sets
the rotation from the area's camera angle:

- Rotation is set once in `Start`, not every frame, because the camera never rotates during play.
  This stays true under the `SideView` perspective camera. Never make billboards track the camera's
  *position*: a fixed card keeps the same width at every screen position, while a tracking one swivels
  as the player walks past it.
- **Yaw only. The card always stands vertical in the world.** There is no tilt field and must never be
  one — see the stretch rule for why.
- Billboards register themselves in a static list in `OnEnable` and remove themselves in `OnDisable`,
  and `SpriteBillboard.RefreshAll()` re-aims the active ones when a camera zone changes.

**CRITICAL — the stretch rule.** A vertical card seen by a camera pitched down by θ is compressed to
cos(θ) of its height. The art compensates for that, and the card never does.

- Draw or render the figure the way it should look on screen, then **stretch the finished image
  vertically** by 1 ÷ cos(pitch) as the last step before export. Width never changes. The pivot stays
  at the point where the figure meets the ground.
  - `ThreeQuarter` (pitch 30): **× 1.155** — a 256 px drawing exports at 296 px.
  - `SideView` (pitch 10): **× 1.015** — a 256 px drawing exports at 260 px.
- The exported file looks too tall and thin when viewed flat, and round heads become ovals. That is
  correct, not a mistake to fix.
- **Pixels Per Unit is calculated from the exported, stretched height**, so that the card's world
  height ends up equal to the character's real height:
  `PPU = exported pixel height ÷ intended world height`. Using the pre-stretch height instead makes the
  card 15.5% too tall in the world, and the character towers over 3D geometry of the same nominal size.
- **Never also tilt the card toward the camera.** Tilting removes the same foreshortening the stretch
  already cancels, so doing both makes every character 15.5% too tall — with no error, no warning, and
  nothing on screen that reads as a bug. This is why `SpriteBillboard` has no tilt field.

**CRITICAL — sprite materials must use alpha clipping, never alpha blending.** Alpha-blended sprites do not write
to the depth buffer, so they sort by object distance and flicker or pop when they overlap — which they
will, constantly. Use a URP Lit or Unlit material with Surface Type Opaque and Alpha Clipping on.
The cost is harder sprite edges; the art should be drawn with that in mind.

**CRITICAL — shadows.** The floor, walls and 3D props cast and receive real shadows. Sprites **do not cast**
shadows (Cast Shadows = Off on the renderer), because a flat card casts a flat card-shaped shadow.
A character or prop that needs to feel planted gets a separate soft blob shadow quad at its base.

The blob quad lies flat on the floor. In `ThreeQuarter` it reads clearly — a flat quad at a 30° pitch
keeps 50% of its depth on screen. In `SideView` at 10° it keeps only 17% and is close to invisible,
which is accepted: those areas show very little floor anyway. The quad therefore carries an Inspector
field **Tilt Angle, default 0**, which lifts its far edge so more of it faces the camera. Visible height
is the quad's depth × sin(tilt + camera pitch), so a `SideView` blob tilted 45° recovers to 82%. The
field exists so that "the character looks like it is floating" is a one-number fix later rather than a
redesign. A shadow quad is a decal, not a character sprite, so it may use a soft alpha-blended
material — it is the one exception to the alpha-clipping rule above.

### Giving the world depth

Four techniques, and the object decides which one:

| Object | Technique |
|---|---|
| Characters and NPCs with animation | billboard + normal map |
| Grass, trees, flowers | billboard + wind |
| Chests, carts, barrels, small buildings — anything read as a solid volume | **a real 3D model**, blocked out in MagicaVoxel and exported as `.obj` |
| Anything that must be correct from every angle | the same: a real 3D model |

**CRITICAL — no sprite stacking.** Stacking a prop out of horizontal slices earns its cost by looking
right from *any* camera yaw. Our yaw is locked at 45° and 0° and §0 rule 5 keeps it there, so every
prop is seen from exactly one angle for the life of the project — and a prop seen from one fixed angle
is a single drawing. Stacking would pay for a freedom this game does not have.

The arithmetic, so nobody has to redo it: `ThreeQuarter` is orthographic at Size 5, which is 108 px per
world unit at 1080p, and a slice spacing of *d* lands `d × cos(30°) × 108` pixels apart on screen. To
keep the banding under one pixel a waist-high chest needs about 65 slices, a cart 112, a small building
280 — and at two pixels of banding, still visible, it is 33 / 56 / 140. A MagicaVoxel model of the same
chest is one mesh, casts a real shadow, receives SSAO, occludes correctly by depth, and is quicker to
make than thirty aligned drawings. (`SideView` is worse, not better: slices read as solid while
`d/D < tan(pitch)`, and tan 10° = 0.176 against tan 30° = 0.577.)

Models are ordinary geometry: they **cast and receive shadows normally**. The no-shadow rule in §0 is
about flat cards, and it does not reach anything with real volume.

**Normal maps on sprites.** The sprite shader writes a fixed normal and tangent in the vertex stage
because `SpriteRenderer` supplies none; a normal map rides along as a Secondary Texture on the sprite,
so it follows automatically when an animation swaps frames.

- **One file per sheet, the same size and layout as the sheet.** A Secondary Texture is sampled with
  the sprite's own UV rect, so a small standalone image lands correctly on the first cell and nowhere
  else. Whatever the map contains, it is painted into every cell of a full-size copy of the sheet.
- **A hand-painted normal map is stretched with its sprite** — the same ×1.155 or ×1.015, exported
  together — and **its green channel is left alone.** The stretch and the camera's foreshortening
  cancel exactly, so the image on screen matches the original drawing, which means the lighting must
  match the original drawing too. Re-baking the slopes would break what the stretch just fixed.
- Normalize after sampling. Resampling and bilinear filtering both leave vectors off unit length.
- **Start with one soft cylinder normal map shared by every frame** — bulging toward the camera at the
  centre, curving away at the edges. It does not match the drawing frame by frame, but it buys the
  thing actually worth having: the figure stops reading as a flat sticker. Per-frame normal maps are a
  trap at this scale: one NPC with eight directions and eight frames is 64 of them.
  - A cylinder is a horizontal gradient and **does not vary along Y, so stretching it does nothing**.
    Build it at the exported sheet's size from the start and skip the stretch step entirely.
  - Check the sheet first: if the figure drifts around inside its cell between frames, the cylinder
    has to be placed per cell rather than tiled, which is still one file but not one paste.

**Wind.** Vertex displacement in the shader, masked so the base holds still and the top moves.

- The mask is **`positionOS.y` with no divisor** — the pivot sits at the feet, and PPU is computed from
  the stretched image, so object-space Y *is* the height above the ground in world units. Scaling a
  card does not break this: the offset is built in object space and scaled by the same matrix as the
  height, so the sway *angle* stays put and the sway distance keeps tracking the card's real world
  height. Tall grass
  therefore sways further than short grass, which is what real grass does, and one material serves
  every height with no script, no `MaterialPropertyBlock`, and no SRP Batcher break. Linear, or
  `pow(max(y, 0), 1.5)`; never squared, which makes a 1.0 blade sway eleven times a 0.3 one. Never
  `UV.y` — a sprite inside an atlas does not span 0–1.
- The displacement must run in **every pass the sprite is drawn in**, including `DepthNormals` (SSAO
  reads it) and the outline pass. Miss the outline pass and the outline stands still while the blade
  sways, which compiles perfectly and looks broken.
- Mesh Type `Tight` gives a sprite 8–20 vertices instead of Full Rect's 4, so the bend is softer. It is
  still a shear, not a curve. Real curvature needs a subdivided mesh, which a `SpriteRenderer` cannot
  provide — plan around that rather than discovering it late. Anything with an outline stays Full Rect:
  a Tight mesh hugs the alpha, leaving no margin for the outline to be drawn in.

**Shader time follows game time.** URP sets `_Time` from `Time.time`, which `timeScale` already scales,
so wind and every other time-driven shader stop on their own when the game pauses. Nothing needs
writing, and a global time uniform must not be added to "fix" it. In Edit mode URP uses
`realtimeSinceStartup` instead, so grass sways in the Scene view with play stopped; that is correct.

**Depth of Field is never used in a `ThreeQuarter` room.** It makes a puzzle harder to read, and URP's
DoF shaders compute distance with `LinearEyeDepth`, a perspective-only formula, so against an
orthographic camera the blur is simply wrong. It is available to `SideView` corridors through that
zone's own Volume.

### Camera

One `CinemachineCamera` per area, following the player, never rotating during play. Projection, angle
and framing come from the area's preset:

| | `ThreeQuarter` | `SideView` |
|---|---|---|
| Projection | Orthographic | Perspective |
| Pitch / Yaw | 30 / 45 | 10 / 0 |
| Size / FOV | Orthographic Size 5 | Field of View 19 |
| Camera distance | 20 | 20 |
| Target Offset | 0, 1.25, 0 | 0, 2, 0 |
| Damping X / Y / Z | 1 / 1 / 1 | 1 / 1 / 1 |

**Every value in this table is written by code from the preset. None of them is adjusted by hand on a
camera in a scene.** A camera whose values disagree with its preset has been edited by mistake and is
put back, not accommodated: the spec is the source, the scene is not.

These numbers are **locked**. The `SideView` field of view was chosen from measured captures: at 19 the
character stands 249 px tall on a 1080-high screen, 23% of the frame, which is the framing the
reference art was drawn against. Changing it later moves every `SideView` sprite's apparent size.

**Damping is the same on all three axes.** The Position Composer damps along the camera's own axes, so
walking into depth excites two of them at once; different values on those axes bow the camera's path
and show up as the picture drifting vertically while the player runs. An earlier version of this file
asked for 1.3 on one axis — that number came from the 2D era, when that axis meant something else, and
it was carried forward without a reason. There is no reason now.

### Camera zones

A scene may be one area, or several areas side by side with a preset each: the player walks out of a
`SideView` corridor through a door into a `ThreeQuarter` puzzle room, with no scene load.

A zone is a `CameraZone` component on a root object that stays enabled, carrying a trigger `BoxCollider`
on the `Ignore Raycast` layer that marks the zone's extent, plus a **Content** child holding everything
else: that zone's `LevelSettings`, its `CinemachineCamera`, its floor, walls and props.

- **Switching zones enables one Content and disables the others.** Nothing touches camera priorities.
  Because the other zone's geometry is switched off it cannot appear in frame, and `LevelSettings`
  stays the only thing that decides a camera's angle.
- **One `CameraZoneSwitcher` per scene** finds which zone box the player stands in and switches. It is
  not a singleton. After switching it calls `SpriteBillboard.RefreshAll()` and then raises
  `OnCameraZoneChanged` — in that order, because the camera must already carry the new angle before
  the billboards aim from it.
- **Walls keep the player inside a zone.** If the player still ends up in no zone at all — a bad spawn
  point, a teleport to the wrong place — the current zone stays on. Never switch everything off: a
  black screen is the one symptom a beginner cannot diagnose.
- **Two zones of different presets must never be visible from each other.** Disabling Content handles
  that for zones built this way, but the layout must not depend on it: zones connect through doors,
  turns, or anything else that blocks the sightline, never across open ground.
- **The transition is a cut behind a fade, never a blend.** Cinemachine cannot blend an orthographic
  camera into a perspective one — `OrthographicSize` and `FieldOfView` are different units, so an
  interpolated value is meaningless and the picture jumps part-way through. The `CinemachineBrain` in
  any scene with zones has **Default Blend = Cut**, and the move is hidden behind the same fade the
  door transitions already use.
- `ZoneDoor : InteractableBase` carries a destination transform and goes through `SceneLoader`, which
  fades out, moves the player, waits for the zone to switch and the camera to cut, then fades back in.
  Click or touch, required item and blocked message all come from the base class for free.

### Level settings

Each area has one `LevelSettings`. It holds the camera preset and the area's play area and applies them
on scene start or when its zone switches on, so the level designer sets one dropdown instead of
remembering numbers. Zoom, field of view, distance, damping and bounds stay overridable in the
Inspector; pitch and yaw do not, and have no Inspector field at all.

**The code that applies a preset to a camera is a `public static` method, not a private method of
`LevelSettings`.** It takes a `CinemachineCamera` and the resolved values and sets the lens mode, size
or FOV, rotation and distance. `LevelSettings` only calls it, and each zone calls it for its own camera.

**The designer draws the play area, not the camera box.** `LevelSettings` holds a box covering the
region the player can be in — something a level designer can see and reason about — and computes the
`CinemachineConfiner3D` volume from it at runtime: inset by half the visible width and depth at the
player's plane, offset by the camera's own height and distance. The visible width comes from
`Camera.aspect`, so the result is right on every monitor and is recomputed if the aspect changes while
the game runs. A camera box placed by hand is correct for exactly one aspect ratio and asks the
designer to do the camera's offset maths, which Intern B cannot be expected to do.

When the computed box comes out zero or negative on an axis the area fits on screen and the camera
should not move on that axis at all — which is correct and common. The box is given a **minimum of
0.01** per axis so the collider is never degenerate. Keep it that small: the camera slides from one
side of that box to the other as the player crosses the middle of the area, and the jump the player
sees is the size of the box.

### Items and inventory

`ItemData` (ScriptableObject): id, localized name, localized description, icon, maxStack, category.
Inventory supports add/remove/count and stacking, and raises `OnInventoryChanged`. The inventory UI
subscribes and refreshes itself; it never touches inventory data directly. **The inventory has no
capacity limit.** A bag too full to hold the key that opens the next door is a dead end in a game where
items gate progress, so the limit does not exist and there is no "inventory full" path to write.

**One `ItemDatabase`, holding every item in the game, loaded at startup and alive for the whole
session.** Items travel between levels — a key found in level 3 is still carried in level 7 — so
`GetById` has to resolve any id at any time, from any scene. A per-level list of items cannot do that:
load a save made in level 7 and the key from level 3 silently disappears, with nothing in the console
to say why.

**Categories are folders and a field, never separate databases.**

```
Data/Items/
  Puzzle/       things that open something: keys, gears, fuses
  Consumable/   things that are used up: food, tonics
  Quest/        things carried to the end of the story
  Secret/       collectibles, see below
```

The folders are what a person browses; the `category` field is what code and the UI read, because a
script cannot look at a folder path while the game is running. Two tests keep them honest: every
`.asset` under `Data/Items/` must appear in `ItemDatabase`, and an item in the `Puzzle/` folder must
have `category = Puzzle`. The first of those catches the most common mistake there is — making an item
and forgetting to register it.

**Consumables carry plain numbers: `healAmount` and `staminaAmount`, where 0 means no effect.** Two
numeric fields beat an effect system for the two effects that exist, and Intern B types a number
instead of creating and dragging an asset. **The day a third kind of effect appears that is not a
number added to a stat** — lighting a torch, revealing the map, slowing time — that is the day this
becomes an `ItemEffect` ScriptableObject with an `Apply` method, one small class per effect. It is not
the day to add a third `if`.

**Secret items are gallery entries, not tools.** They carry a localized title, a localized body, and an
optional image, and they are shown in an extras screen outside the levels; found ones are revealed and
the rest stay silhouettes. Picking one up unlocks its gallery entry and does **not** put it in the bag,
which is kept for things the player can act with.

**CRITICAL — an item's `id` can never change once the game has shipped a save.** Saves store ids, so
renaming `key` to `rusty_key` makes every existing save fail to find it, silently. Display names and
descriptions can change freely at any time; the id cannot. If `GetById` is handed an id it does not
know — an item deleted from a later build — it skips that entry and logs a warning naming the id. It
never throws, and it never passes over it in silence.

### Holding an item

Some puzzles need the player to have a particular thing *in hand*, not merely somewhere in the bag.

**One item is held at a time, and what is held is a kind of item, not a particular one.** Holding does
not take the item out of the bag: `Count` is unchanged, so a door that only asks whether the player
*has* a key still opens while that key is held. The held item is part of the gameplay save, so it is
still in hand after a reload, and if the last of that item leaves the bag the hand empties itself.
Changes raise `OnHeldItemChanged`.

**On screen the hand is a slot of its own**, both inside the inventory screen and as a small slot
beside the bag button, so the player can see what they are holding without opening anything.

**The bag shows one fewer of whatever is held.** Hold the only key and it leaves the bag and appears in
the hand; hold one of three potions and the bag shows two. Hiding the whole stack while one is held
would tell a player with three potions that they have none.

### The inventory screen

Hand slot on the left, bag on the right: a grid five columns wide, **at least 25 cells, always drawn
even when empty**, growing five at a time and scrolling if the player ever carries more. The 25 cells
are a minimum to fill the artwork, never a capacity — nothing is ever refused (see above). Opened by
the button at the bottom right of the screen or by **I**, and the game pauses while it is open.

Clicking an item in the bag takes it into the hand; if something was already held it goes back to the
bag. Clicking the hand slot puts it away. **The UI never edits inventory data**: it calls `Hold` and
`StopHolding` and redraws from `OnInventoryChanged` and `OnHeldItemChanged`, like every other screen
in §3.

**Canvas Scaler is Scale With Screen Size, 1920×1080, Match 0.5** — not Constant Pixel Size. The game
supports wide and high-resolution monitors, and a UI that does not scale is unreadably small on them.
Fix this before building a screen, never after: every panel laid out against the wrong scaler has to be
laid out again.

### Dialogue

Three types.

- **Full** — a portrait on each side of the screen with the text box across the bottom between them.
  **The player is always on the left and the other character always on the right** for the whole
  conversation; they never swap sides. Whoever is speaking is at full brightness and the other is
  dimmed, and the speaker's name sits on the text box. Click to advance. Blocks movement.
- **Choice** — the same layout plus Yes/No buttons with different outcomes via UnityEvent. Blocks movement.
- **Mini** — a small bubble on a World Space canvas above the object or the player's head, fades out by
  itself, does not block movement.

**Which one to use is not a style choice.** Getting this wrong is how a chest that turns out to be
empty ends up stopping the game with a full-screen box.

| | Use it for | Examples |
|---|---|---|
| **Mini** | an object's reaction to being touched: no speaker, no choice, no consequence | "Empty." · "It's locked." · "Too heavy to move." · "Got the key." |
| **Full** | a conversation with a character, or story text the player is meant to read | an NPC talking, a letter, the thought that opens a level |
| **Choice** | only when the answer changes what happens next | "Open it anyway?" with real consequences either way |

`blockedMessage` on `InteractableBase` always goes to **Mini**. Telling the player they need a key is
not a conversation and must not stop them walking.

**Every message field anywhere in the project holds a dialogue asset dragged into it, never typed
text.** A field with Thai typed straight into it works perfectly until the day the game is translated,
and by then there are hundreds of them scattered across prefabs where no translator can find them. An
asset reference also cannot be misspelled, and Unity reports it if the asset goes missing. This costs
nothing to get right at five objects and is very expensive at two hundred.

Three fields do not follow this yet — `blockedMessage`, `PickupItem.pickupMessage` and
`MessageInteractable.message` — because changing a field's type drops the text already typed into
scenes. They are converted in one deliberate pass that reads those strings out first, not quietly
alongside other work. New fields follow the rule from the start.

Content is authored as JSON by a writer and imported into ScriptableObjects. `OnDialogueStarted` /
`OnDialogueEnded` lock and release player input.

**Stamina.** 0–100. Drains only while running. No regeneration while running; regenerates while walking or idle. At 0 the player is forced to walk and cannot run until stamina reaches **30**. Feedback when depleted: bar flash or shake plus a sound. Raises `OnStaminaChanged`.

**Health and damage.** `IDamageable` with `TakeDamage(int amount)`. Traps and monsters call it through triggers. Raises `OnHealthChanged`; at 0 raises `OnGameOver`.

**Timer.** A countdown used by some puzzles only, not every level. On timeout it plays a Fail animation (Animator trigger name set in the Inspector) and raises `OnGameOver`. It stops while the game is paused.

**Freeze time.** Opening the menu or inventory sets `Time.timeScale = 0`. UI still works while paused, using unscaled time.

**Save and load.** Two JSON files in `Application.persistentDataPath`. *Gameplay save*: inventory (item ids and counts), current scene, player position, timer value, completed puzzle ids, used one-shot ids. *Global save*: settings, **and the ids of secret items the player has found**. Autosave on scene load, item pickup and level complete. Continue loads on game start.

**Found secrets belong to the player, not to the save slot.** They live in the global save so that
starting a new game does not wipe a collection the player spent the whole game building. Getting this
backwards cannot be fixed later, because by then the loss has already happened on their machine.

**A one-shot's saved id is `<scene name>/<its Unique Id>`.** Scoping the id to its scene means an id
only has to be unique within one scene, which is something the game can actually check while it is
running; with globally unique ids, two objects in different scenes sharing a name would quietly mark
each other as used and nothing could detect it. The cost is that **a scene's name becomes part of the
save format**: renaming a level scene after the game has shipped invalidates every one-shot in it.
Rename scenes freely now; treat them as frozen once players have saves.

**The check for duplicate ids runs once at scene start, from one place, and includes objects that are
switched off** (`FindObjectsByType<InteractableBase>(FindObjectsInactive.Include, …)`). A check that
each object runs in its own `Start` never sees the zones that are still closed, so a duplicate inside
one only reports itself when a player happens to walk in — which is usually after the build has gone out.

**Restoring must be safe to do twice.** An object may be restored both when the scene becomes ready and
when its zone opens for the first time, so restoring sets state rather than toggling it, and never
consumes anything. A restored object does not play its own sound or show its own message — but a
`UnityEvent` wired to it fires whatever it is wired to, so a sound belongs in the component's own sound
field, not dragged into the event, or it replays on every load.

**Anything that happens once must be saved as having happened.** A chest that gave up its key, a
message that only plays the first time, a door that unlocked — each has an id, and the id goes into the
save the moment it is used. On scene ready, `InteractableBase` checks its own id against that list and
switches itself off if it is there, exactly as `PuzzleBase` restores itself (§8). Without this the
player reloads a save and finds the key back in the chest, and can farm it; the symptom appears in
every level that has anything to pick up, so it is not a detail to leave for later.

**Settings.** Music and Sound sliders through an AudioMixer. Brightness through a URP Global Volume, Color Adjustments → Post Exposure: slider minimum **10** is the normal default (Post Exposure 0), and the slider maximum is an Inspector field clamped between **50 and 70**. Language dropdown. All stored in the global save. Nothing outside the settings system calls the brightness code — a door or a puzzle that sets brightness is a bug, not a feature.

**Highlight.** A soft white outline on interactable objects via URP Shader Graph, following the object's silhouette. Shown when the player is near, and also when the player has not interacted with anything for a configurable idle time (hint system).

**Cutscenes.** Either a Timeline (`PlayableDirector`) or a video clip (`VideoPlayer`), with a Skip button. Used for: the storybook intro, the start of the game, and before collecting the puzzle gem. Player input is locked while one plays.

**Scene flow and game over.** Doors carry a target scene and a spawn point ID and go through `SceneLoader`. `GameManager` persists inventory and state across scenes. Game over: optional Fail animation → fade to black → wait a delay (Inspector field, 2–5 seconds) → load the last save. One implementation used by both death and timeout.

---

## 8. Extension points for the interns

These exist so interns can add content without touching core code.

- `InteractableBase` — subclass it, override `OnInteract`. That is the whole job.
- `MiniGameBase` — shows its UI, locks player input while it runs, returns success or failure, closes itself.

### `ContainerInteractable` — and the rule that keeps this folder small

A chest, a crate, a drawer, a bookshelf. One class, and the differences between any two of them live in
the Inspector, never in code:

- **Contents** — an `ItemData`, **and it is allowed to be empty**
- **Amount**
- **Empty message / Taken message** — each a dialogue asset dragged in, shown as Mini dialogue

```
Contents is empty            -> Mini (Empty message), and that is all
otherwise                    -> give the item, Mini (Taken message), mark this id used
```

Two paths, not three. There is no "bag is full" case because the inventory has no capacity (§7).

That is the whole class. An empty chest is not a different kind of chest; it is this chest with one
field left blank. A chest holding a key and a chest holding a torch are the same prefab with a
different asset dragged in. Two hundred chests in the finished game are two hundred instances of one
prefab, configured by Intern B, with no code written for any of them.

**The rule, in order. Do not skip a step.**

| What is needed | How | Who |
|---|---|---|
| different contents, different words | change the fields | Intern B |
| something extra happens too — a sound, a door opening, an animation | wire the `OnOpened` UnityEvent | Intern B |
| genuinely new logic that no combination of fields and events can express | a new subclass | Intern A |

The UnityEvent is the relief valve, and it is the reason this folder will not fill up with
`Box_001`-style classes. Before writing the sixth subclass of anything, say out loud why fields and an
event cannot do it. Usually they can.

**A unique id per object is required; a unique class per object is a debt.** Every chest needs its own
id so the save system knows which ones are already open (§7). Ids can number in the thousands at no
cost. Classes cannot.
- `QuickTimeEvent : MiniGameBase` — Inspector-configurable: ordered input actions, time limit per input, allowed mistakes, prompt prefab. A new QTE should need no new code.
- `LevelSettings` — one per area. Holds the camera preset and play area and configures the area on start.
- `SpriteBillboard` — drop it on any sprite that should face the camera. No configuration needed in the normal case.

### `PuzzleBase`

`puzzleId`, `oneTimeOnly`, `requiredPuzzleIds`, `StartPuzzle()`, `CompletePuzzle()`, `FailPuzzle()`, plus
UnityEvents `OnPuzzleStarted` / `OnPuzzleCompleted` / `OnPuzzleFailed` so Intern B can wire doors,
sounds and animations in the Inspector with no code.

A level holds about five puzzles: one large and four small. The small ones may stand alone, or may
together unlock the large one. Two rules make that work:

- **`requiredPuzzleIds`** — an Inspector list of puzzle ids that must already be complete before this
  one can start. Empty means available immediately. A puzzle that is not yet available shows its
  `blockedMessage` instead of starting.
- **Silent restore.** A puzzle is solved once and never replayed. Completion is recorded by the save
  system through `OnPuzzleCompleted(puzzleId)`. On scene ready, a puzzle whose id is already in the
  completed set **re-fires its `OnPuzzleCompleted` UnityEvent**, so the door it opened is open again,
  but skips sound, animation and dialogue. Without this the save knows the puzzle is done while the
  door it opened is a freshly loaded object sitting closed, and the player is locked in a room with
  nothing left to solve. Intern B wires the event the same way either way; nobody has to think about
  saving at all.

### Asking whether the player is holding the right thing

A puzzle that needs an item in hand has three ways to check, and the first one that fits is the one to
use. Only the third needs code.

| | How | Who |
|---|---|---|
| On any interactable | Set `Required Item` and tick **Must Be Held**. Without it in hand the interactable shows its message and does nothing. | Intern B |
| Inside a puzzle screen | Add a `HeldItemCheck`: set `Required Item`, wire `On Correct` and `On Wrong`, and call `Check()` from a button. | Intern B |
| In code | `player.Inventory.IsHolding(item)`, read `HeldItem`, listen to `GameEvents.OnHeldItemChanged`. | Intern A |

**"You need a key" and "you need to be holding the key" are different sentences.** An interactable that
checks for a held item carries a second message for the case where the player has the item but is not
holding it; with one message, a player holding the key in their bag is told they do not have it, and
has no way to work out what the game wants. The second message falls back to the first when left empty.

**CRITICAL — an item that is used up must be the last thing a puzzle needs.** `HeldItemCheck` can
consume the item it checked, and it does so only on success. But a puzzle that consumes something at
step one and then fails at step two has eaten the only way through itself: the save records the item as
gone, the puzzle is still unsolved, and the player is stuck with nothing on screen to explain it. Either
consume at the final step, or make sure the puzzle can still be finished without that item.

Gameplay code **subscribes** to `GameEvents` but never raises core events.

**Hard stops for gameplay work.** If a task requires any of these, stop and report it instead of writing code:
writing `Time.timeScale`; calling `SaveManager` or needing state to survive a scene reload; raising a core event; locking or unlocking player input; adding a field or changing a signature in a core class; adding a new event to `GameEvents`; pathfinding or monster AI; creating an asmdef; changing a camera preset's values or adding a third preset; adjusting a camera in a scene by hand; changing brightness from outside the settings system; writing any code that sorts sprites by position or scales them by distance.

---

## 9. Testing

Required for every system.

- **EditMode unit tests** for pure C# logic, where most tests belong: inventory add/remove/stack limits, stamina math and the 30 threshold, save data round-trip, localization lookup, timer countdown, puzzle state and gating, camera-relative direction maths, facing snapped to 8 directions, `CameraZone.Contains`, and `GameEvents` subscribe/unsubscribe.
- **PlayMode integration tests** only for behaviour that needs the engine: click vs touch interaction, inventory surviving a scene load, a zone switch leaving exactly one `LevelSettings` active, game over reloading the save, input locked during dialogue.
- Do **not** test Unity itself. Never assert that `transform.position` changed after setting it.
- **Camera-relative movement must be tested at yaw 45, not only at yaw 0.** At the `SideView` yaw of 0
  the camera axes and the world axes coincide, so that case passes even if the code ignores the camera
  completely. Only the `ThreeQuarter` yaw of 45 can fail. Keep both cases; the yaw-45 one is the test.
- Do not write a test you could not make fail by breaking the production code.
- Every fixed bug gets a regression test that would have caught it.
- Tests live in `Assets/_Project/Tests/` (PlayMode) and `Assets/_Project/Tests/Editor/` (EditMode). Test Runner's "Enable playmode tests for all assemblies" must be turned on, because game code has no asmdef. Do not add an asmdef for game code to make tests compile — ask first.
- **Read `playModeTestRunnerEnabled` in `ProjectSettings/ProjectSettings.asset` and report the value you
  actually saw.** While it is `0`, do not write test files into the project: put the test code in the
  report and say the setting is still off. Once it reads `1`, write the files and run them.
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

- One `CinemachineBrain` on the Main Camera. One `CinemachineCamera` per area follows the player.
- The projection depends on the area's preset: **Orthographic** for `ThreeQuarter`, **Perspective** for
  `SideView`. The camera is at a fixed angle and never rotates during play.
- **`Default Blend` on the Brain is `Cut` in any scene that has camera zones.** An orthographic and a
  perspective camera cannot be blended; a timed blend makes the picture jump part-way through instead
  of cutting cleanly. It is a per-scene Inspector value, so every such scene needs it set.
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
  created by an intern with one checkbox, in an area where every sprite was drawn for a different one.
- **`LevelSettings` resolves preset plus overrides in exactly one place, and everything reads it from
  there.** `Apply`, `CameraPitch` and `CameraYaw` all take their values from that one method; nothing
  calls the raw preset lookup directly. `SpriteBillboard` aims from `CameraPitch` and `CameraYaw`, so
  the moment those can disagree with what `Apply` gave the camera, every sprite in the area aims at an
  angle the camera is not at — and it looks like bad art, not like a bug, so nobody reports it.
  The test is end-to-end: after `Apply`, the camera's actual rotation must equal `CameraPitch` and
  `CameraYaw`.
- Camera bounds use `CinemachineConfiner3D` with a `BoxCollider` marked Is Trigger on the
  `Ignore Raycast` layer. The box limits where the **camera** may sit, not where the player may walk.
  It is computed at runtime from the area's play area — see §7 Level settings — never placed by hand.
- **A confiner hides a broken camera instead of revealing it.** When a camera's angle or target offset
  is wrong, the position it asks for usually falls outside the confiner box, so the confiner clamps it
  to a corner of that box every frame. The result looks plausible, is completely wrong, and the camera
  has stopped following the player entirely. When a camera looks off, read its preset values first and
  compare its actual position with where the preset says it should be, before changing anything.
- **Under a perspective camera the visible width grows with distance, so size the back wall for the
  far plane, not the player plane.** Work out the half-width at the back wall's depth, add the camera's
  maximum sideways travel, and make the wall at least that wide — at the widest aspect ratio the game
  supports, not just 16:9. A back wall sized for the player plane leaves a visible gap at the ends of
  the area once the camera pans to either extreme.
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

### Sub-areas inside a level

A level is one scene containing several sub-areas (1.1, 1.2, 1.3 …). Clearing all of them loads the
next level's scene. Doors inside a scene move the player between sub-areas; doors between levels go
through `SceneLoader` as they do today.

**Each sub-area will be one parent GameObject, deactivated until the player enters it** — the same
shape as a camera zone's Content, one level up. A scene holding five sub-areas otherwise loads all five
at once, which is more in memory than five separate scenes, not less.

This works because of §3: deactivating an area disables its scripts, which unsubscribe in `OnDisable`,
and re-enabling resubscribes. The one rule it adds — **state belonging to a sub-area lives in a manager,
never on a GameObject that gets deactivated**, because `OnEnable` runs again and local state is lost.

### Puzzle ownership

`PuzzleManager` in the gameplay folder uses a static `Instance`, which §3 forbids and which breaks the
moment a level holds two puzzles at once — and a level is specified to hold about five: one large and
four small (§8).

**This file belongs to the intern who wrote it, and the change is theirs to make.** Nobody else edits
it. What matters is the deadline: it has to become one instance per puzzle **before a second puzzle is
written against the current shape**. After that, two puzzles have to be unpicked instead of one, and
the second was built on a foundation that was already known to be wrong.

### Making §5 true on disk

Three things sit in the wrong place. None of them move before the intern branch is merged, because two
are the intern's files and a move during an open branch is a merge conflict in the worst possible form —
a file that exists twice. At merge time they move, in this order, **with `AssetDatabase.MoveAsset` and
nothing else**, recording every GUID before and after:

| From | To | Why it matters |
|---|---|---|
| `Assets/Inventory/ItemData.cs` | `Scripts/Items/` | Core code outside the core tree. This is the file that already lost its GUID once (`dc8e31f`) and took three assets down with it. |
| `Assets/Puzzle/PuzzleManager.cs` | `Scripts/Gameplay/Puzzles/` | Same move as the singleton fix above; do both at once rather than touching the file twice. |
| `Assets/Puzzle/` scenes and art | `Scenes/` and `Art/` | Last, and only once the two above are verified, because scenes are what break. |

`Scripts/Gameplay/` does not exist yet. It gets created on first use, from Unity's Project window, not
by hand on disk.