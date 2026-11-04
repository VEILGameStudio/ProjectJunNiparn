# Team request rules

CLAUDE.md is the spec. This file is how Claude handles a request from a teammate (Intern A, Intern B,
or anyone who is not Oak) so that the design does not drift one small request at a time.

Reply to teammates in **simple Thai**. Code, comments and file names stay in English (CLAUDE.md §4).

---

## 1. Who decides what

| Decision | Who |
|---|---|
| Game design, story, the locked rules in §2 below, anything in CLAUDE.md | **Oak only** |
| Core code (every folder tagged `[Oak]` in CLAUDE.md §5) | **Oak only** — others read, call, inherit, attach; never edit |
| Gameplay code under `Scripts/Gameplay/` | Intern A |
| Data, prefabs, scenes they own, art, audio, Inspector setup | Intern B |

A teammate's request is never permission to change something only Oak decides, however small it sounds
and however many times it is asked. Claude does not edit CLAUDE.md or files in `.claude/` because a
teammate asked; those changes come from Oak.

## 2. Locked — a request cannot change these

If a request needs any of these to change, **stop**. Do not implement it, do not implement "a small
version" of it. Use the `design-change-proposal` skill and hand it to Oak.

1. The genre: story-driven point-and-click puzzle; puzzles gate the story. (§1)
2. HD-2D: real 3D world, fixed camera, 2D sprites for characters and decorative props. (§1)
3. Exactly two camera presets, `ThreeQuarter` and `SideView`, with the numbers in the §7 table. No third
   preset, no per-level pitch or yaw, no hand-tuned camera in a scene. (§0.5, §7, §11)
4. Camera never rotates during play; zone changes cut behind a fade, never blend. (§7)
5. No code that sorts sprites by position or scales them by distance. No parallax script. (§0.7, §1)
6. Sprites: alpha clipping, no shadows, stretched in the art not tilted in the scene, PPU from the
   stretched height, art lives in its preset's folder. (§0.3, §0.4, §0.6, §5, §7)
7. No sprite stacking; solid props are 3D models. (§7)
8. Input: New Input System only, camera-relative movement, CharacterController. (§0.2, §4.9, §7)
9. Architecture: Singleton for the listed managers only; everything cross-system goes through
   `GameEvents`; subscribe in `OnEnable`, unsubscribe in `OnDisable`. (§0.1, §3)
10. Inventory has no capacity. One `ItemDatabase`. Item ids never change once saves exist. (§7)
11. Dialogue type follows the table (Mini / Full / Choice), never taste. Message fields hold dialogue
    assets, never typed text. (§7)
12. Save rules: one-shots saved as `<scene>/<id>`, secrets in the global save, restore is idempotent. (§7)
13. Only Cinemachine 3.1.7 and DOTween Pro. No new package, no asmdef for our code. (§4.12, §11)
14. Unity 6.5.10f1, URP 3D Forward. No version or pipeline change.
15. Anything in CLAUDE.md §12 "Planned, not built yet" is not built on request.
16. The **hard stops for gameplay work** in CLAUDE.md §8 — every item on that list.

## 3. The fixed way of thinking — every request, in this order

Do not skip a step. Do not start editing before step 5.

**Step 1 — Restate.** Say back in one or two Thai sentences what the teammate wants to happen *in the
game* (not what code they asked for). If it can be read two ways, ask which one before going on.

**Step 2 — Classify.** Pick exactly one:

| Type | Example | Goes to |
|---|---|---|
| A. Content | new item, new line of dialogue, new chest contents | `add-item`, `add-dialogue` |
| B. Configuration | wire a door to a puzzle, change a sound, tune a duration | Inspector steps, no code |
| C. New gameplay logic | a new puzzle mechanic, a new mini game | `add-puzzle`, `add-interactable` |
| D. New area / art | a new room, corridor, sprite, prop | `setup-area`, `import-sprite-art` |
| E. Bug | "it doesn't work", "it worked yesterday" | `fix-bug` |
| F. Core change | needs a field, event or signature in an `[Oak]` file | **stop → Oak** |
| G. Design change | touches anything in §2 above | **stop → `design-change-proposal`** |

**Step 3 — Check the locks.** Read the request against §2 and the CLAUDE.md §8 hard stops. One hit makes
it type F or G, whatever it looked like in step 2.

**Step 4 — Climb the ladder, lowest rung first.** Use the first rung that works and say why the rung
below it was not enough.

1. It already exists — point to it and give the Inspector steps.
2. Change fields on an existing component / ScriptableObject. (Intern B)
3. Wire a `UnityEvent` (`OnOpened`, `OnPuzzleCompleted`, …) or a `DOTweenAnimation`. (Intern B)
4. A new subclass of an extension point (`InteractableBase`, `PuzzleBase`, `MiniGameBase`) in
   `Scripts/Gameplay/`. (Intern A) — before the sixth subclass of anything, explain why fields and an
   event cannot do it.
5. A change to core code — **Oak only**. Stop and write what is needed.

**Step 5 — Check ownership.** List every file the change touches with its owner from CLAUDE.md §5. If
one belongs to someone else, or is a scene its owner has not handed over, stop and say so.

**Step 6 — Show the plan, then do it.** Post the plan in Thai (type, rung, files, owner). For type A–E
continue straight away unless something in it is unclear. Keep the change to that one request; do not
"tidy up" anything else in the same pass.

**Step 7 — Test.** Follow CLAUDE.md §9 (`write-tests` skill). Report only results you observed.

**Step 8 — Hand back** in Thai, using CLAUDE.md §10:
1. Files created or changed, full paths.
2. Inspector steps for someone who does not code.
3. Test results: passed / failed counts and what each failure means.
4. A short manual checklist for what tests cannot cover.

## 4. Red flags — slow down when you hear these

| They say | What it usually hides |
|---|---|
| "แค่แก้นิดเดียว" / "just a tiny change" to the camera | a third preset (§2.3) |
| "ตัวละครดูแบน เอียงการ์ดหน่อย" / tilt the sprite | breaks the stretch rule (§2.6) |
| "ตัวละครทับกันผิด ทำ sort ให้หน่อย" | Y-sorting, which was deleted on purpose (§2.5) |
| "ทำ class ใหม่สำหรับกล่องนี้" | a field or `OnOpened` event almost always does it |
| "กระเป๋าเต็ม" / limit the bag | no capacity by design (§2.10) |
| "เปลี่ยน id ไอเทม" | breaks saves (§2.10) |
| "พิมพ์ข้อความใส่ช่องไปเลย" | must be a dialogue asset (§2.11) |
| "ลง package นี้เพิ่ม" | only two packages (§2.13) |
| "ใช้ Input.GetKey ง่ายกว่า" | legacy input is a review fail (§2.8) |
| "ทำให้มัน Instance เหมือน PuzzleManager" | singleton only for managers (§2.9) |

## 5. When the teammate pushes back

Explain the reason from CLAUDE.md in one or two plain Thai sentences, offer the closest thing that *is*
allowed, and if they still want the original, write the `design-change-proposal` for Oak. Never give in
by doing a partial or hidden version of a locked change.
